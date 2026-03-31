using System.Collections.Immutable;
using LayeredCraft.OptimizedEnums.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayeredCraft.OptimizedEnums.Generator.Providers;

internal static class EnumSyntaxProvider
{
    private const string OptimizedEnumBaseMetadataName = "LayeredCraft.OptimizedEnums.OptimizedEnum`2";

    internal static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax { BaseList: not null };

    internal static EnumInfo? Transform(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Node is not ClassDeclarationSyntax classDecl)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken)
            is not { } classSymbol)
            return null;

        // Only generate for types that actually inherit from OptimizedEnum<TEnum, TValue>
        var baseType = FindOptimizedEnumBase(classSymbol, context.SemanticModel.Compilation);
        if (baseType is null)
            return null;

        var diagnostics = new List<DiagnosticInfo>();
        var location = classDecl.CreateLocationInfo();
        var className = classSymbol.Name;

        // OE0001: Must be partial
        var isPartial = classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.MustBePartial,
                location,
                className));

            return new EnumInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                MemberNames: EquatableArray<string>.Empty,
                ContainingTypeNames: EquatableArray<string>.Empty,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // Extract TValue (second type argument of OptimizedEnum<TEnum, TValue>)
        var valueTypeSymbol = baseType.TypeArguments[1];
        var valueTypeFullyQualified = valueTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        cancellationToken.ThrowIfCancellationRequested();

        // OE0101: Warn about non-private constructors
        foreach (var ctor in classSymbol.Constructors)
        {
            if (ctor.IsImplicitlyDeclared)
                continue;
            if (ctor.DeclaredAccessibility != Accessibility.Private)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.NonPrivateConstructor,
                    ctor.CreateLocationInfo(),
                    className));
            }
        }

        // Scan members for public static fields of the enum type
        var validMembers = new List<string>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol field)
                continue;
            if (field.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (!field.IsStatic)
                continue;

            if (!SymbolEqualityComparer.Default.Equals(field.Type, classSymbol))
                continue;

            // OE0102: non-readonly public static field of enum type
            if (!field.IsReadOnly)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.NonReadonlyField,
                    field.CreateLocationInfo(),
                    field.Name,
                    className));
                continue;
            }

            // OE0006: duplicate member name
            if (!seenNames.Add(field.Name))
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.DuplicateName,
                    field.CreateLocationInfo(),
                    className,
                    field.Name));
                continue;
            }

            validMembers.Add(field.Name);
        }

        // OE0005: duplicate values (best-effort, only for compile-time constants)
        DetectDuplicateValues(classSymbol, context.SemanticModel, validMembers, diagnostics, className, cancellationToken);

        // OE0004: no valid members
        // Abstract classes with no eligible members are intermediate base classes — skip silently.
        // Abstract classes that do declare members (e.g. via nested concrete implementations) are
        // still valid enums and should generate normally.
        if (validMembers.Count == 0)
        {
            if (classSymbol.IsAbstract)
                return null;

            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.NoMembersFound,
                location,
                className));
        }

        return new EnumInfo(
            Namespace: GetNamespace(classSymbol),
            ClassName: className,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValueTypeFullyQualified: valueTypeFullyQualified,
            MemberNames: validMembers.ToEquatableArray(),
            ContainingTypeNames: GetContainingTypeDeclarations(classSymbol),
            Diagnostics: diagnostics.ToEquatableArray(),
            Location: location);
    }

    private static INamedTypeSymbol? FindOptimizedEnumBase(
        INamedTypeSymbol classSymbol,
        Compilation compilation)
    {
        var optimizedEnumBase = compilation.GetTypeByMetadataName(OptimizedEnumBaseMetadataName);
        if (optimizedEnumBase is null)
            return null;

        var current = classSymbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, optimizedEnumBase))
                return current;
            current = current.BaseType;
        }

        return null;
    }

    private static void DetectDuplicateValues(
        INamedTypeSymbol classSymbol,
        SemanticModel semanticModel,
        List<string> memberNames,
        List<DiagnosticInfo> diagnostics,
        string className,
        CancellationToken cancellationToken)
    {
        // Build a mapping of constant value -> first field name (best effort, skips non-literals).
        // Use object keys so that value-type equality (e.g. decimal 1.0m == 1.00m) is respected
        // rather than their differing string representations.
        var valueToField = new Dictionary<object, string>();
        var memberSet = new HashSet<string>(memberNames, StringComparer.Ordinal);

        // Iterate ALL partial declarations so that members defined in other files are covered.
        foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax(cancellationToken) is not ClassDeclarationSyntax partialDecl)
                continue;

            foreach (var member in partialDecl.Members)
            {
                if (member is not FieldDeclarationSyntax fieldDecl)
                    continue;

                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fieldName = variable.Identifier.Text;
                    if (!memberSet.Contains(fieldName))
                        continue;

                    if (variable.Initializer?.Value is not (
                        ObjectCreationExpressionSyntax or
                        ImplicitObjectCreationExpressionSyntax))
                        continue;

                    ArgumentSyntax? firstArg = variable.Initializer.Value switch
                    {
                        ObjectCreationExpressionSyntax oce => oce.ArgumentList?.Arguments.FirstOrDefault(),
                        ImplicitObjectCreationExpressionSyntax ioce => ioce.ArgumentList?.Arguments.FirstOrDefault(),
                        _ => null
                    };

                    if (firstArg is null)
                        continue;

                    var constantValue = semanticModel.GetConstantValue(firstArg.Expression, cancellationToken);
                    if (!constantValue.HasValue || constantValue.Value is null)
                        continue;

                    if (!valueToField.TryAdd(constantValue.Value, fieldName))
                    {
                        var fieldSymbol = classSymbol.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
                        diagnostics.Add(new DiagnosticInfo(
                            DiagnosticDescriptors.DuplicateValue,
                            fieldSymbol?.CreateLocationInfo(),
                            className,
                            valueToField[constantValue.Value],
                            fieldName));
                    }
                }
            }
        }
    }

    private static EquatableArray<string> GetContainingTypeDeclarations(INamedTypeSymbol symbol)
    {
        var result = new List<string>();
        var current = symbol.ContainingType;
        while (current is not null)
        {
            var keyword = (current.IsRecord, current.TypeKind) switch
            {
                (true, TypeKind.Struct) => "record struct",
                (true, _) => "record",
                (_, TypeKind.Struct) => "struct",
                (_, TypeKind.Interface) => "interface",
                _ => "class"
            };
            // Include static modifier and type parameters so the generated partial declaration
            // matches the original (e.g. static partial class Outer or partial class Outer<T>).
            var staticModifier = current.IsStatic ? "static " : "";
            var nameWithTypeParams = current.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            result.Insert(0, $"partial {staticModifier}{keyword} {nameWithTypeParams}");
            current = current.ContainingType;
        }
        return result.ToEquatableArray();
    }

    private static string? GetNamespace(INamedTypeSymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();
}
