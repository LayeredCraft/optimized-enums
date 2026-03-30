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
        DetectDuplicateValues(classSymbol, classDecl, context.SemanticModel, validMembers, diagnostics, className, cancellationToken);

        // OE0004: no valid members
        if (validMembers.Count == 0)
        {
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
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        List<string> memberNames,
        List<DiagnosticInfo> diagnostics,
        string className,
        CancellationToken cancellationToken)
    {
        // Build a mapping of field name -> constant value (best effort, skips non-literals)
        var valueToField = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var member in classDecl.Members)
        {
            if (member is not FieldDeclarationSyntax fieldDecl)
                continue;

            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fieldName = variable.Identifier.Text;
                if (!memberNames.Contains(fieldName))
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

                var key = constantValue.Value.ToString()!;
                if (!valueToField.TryAdd(key, fieldName))
                {
                    var fieldSymbol = classSymbol.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
                    diagnostics.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.DuplicateValue,
                        fieldSymbol?.CreateLocationInfo(),
                        className,
                        valueToField[key],
                        fieldName));
                }
            }
        }
    }

    private static string? GetNamespace(INamedTypeSymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();
}
