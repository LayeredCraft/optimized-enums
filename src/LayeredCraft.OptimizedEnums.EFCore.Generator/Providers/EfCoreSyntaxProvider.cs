using LayeredCraft.OptimizedEnums.EFCore.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.EFCore.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayeredCraft.OptimizedEnums.EFCore.Generator.Providers;

internal static class EfCoreSyntaxProvider
{
    internal const string AttributeMetadataName =
        "LayeredCraft.OptimizedEnums.EFCore.OptimizedEnumEfCoreAttribute";

    private const string OptimizedEnumBaseMetadataName =
        "LayeredCraft.OptimizedEnums.OptimizedEnum`2";

    internal static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax;

    internal static EfCoreInfo? Transform(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetNode is not ClassDeclarationSyntax classDecl)
            return null;

        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        var attr = context.Attributes[0];
        var diagnostics = new List<DiagnosticInfo>();
        var location = classDecl.CreateLocationInfo();
        var className = classSymbol.Name;

        // OE3004: abstract classes are not supported
        if (classSymbol.IsAbstract)
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.UnsupportedTarget,
                location,
                $"[OptimizedEnumEfCore] cannot be applied to abstract class '{className}'. Apply the attribute to concrete sealed partial derived classes."));

            return new EfCoreInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                ValueTypeIsReferenceType: false,
                ContainingTypeSimpleNames: EquatableArray<string>.Empty,
                ContainingTypeDeclarations: EquatableArray<string>.Empty,
                Storage: EfCoreStorage.ByValue,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // OE3004: enums nested inside generic containing types cannot have converters generated.
        // Converters and extension methods are emitted at namespace scope; generic type parameters
        // from the containing type would not be in scope there, producing uncompilable references.
        var genericContainer = FindGenericContainingType(classSymbol);
        if (genericContainer is not null)
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.UnsupportedTarget,
                location,
                $"[OptimizedEnumEfCore] cannot be applied to '{className}' because its containing type '{genericContainer.Name}' is generic. EF Core converter generation for enums nested inside generic types is not supported in v1."));

            return new EfCoreInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                ValueTypeIsReferenceType: false,
                ContainingTypeSimpleNames: EquatableArray<string>.Empty,
                ContainingTypeDeclarations: EquatableArray<string>.Empty,
                Storage: EfCoreStorage.ByValue,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // OE3001: must inherit from OptimizedEnum<,>
        var baseType = FindOptimizedEnumBase(classSymbol, context.SemanticModel.Compilation);
        if (baseType is null)
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.MustInheritOptimizedEnum,
                location,
                className));

            return new EfCoreInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                ValueTypeIsReferenceType: false,
                ContainingTypeSimpleNames: EquatableArray<string>.Empty,
                ContainingTypeDeclarations: EquatableArray<string>.Empty,
                Storage: EfCoreStorage.ByValue,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // OE3002: must be partial
        if (!classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.MustBePartial,
                location,
                className));

            return new EfCoreInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                ValueTypeIsReferenceType: false,
                ContainingTypeSimpleNames: EquatableArray<string>.Empty,
                ContainingTypeDeclarations: EquatableArray<string>.Empty,
                Storage: EfCoreStorage.ByValue,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // Read Storage from the attribute constructor argument (defaults to ByValue = 0)
        var storage = EfCoreStorage.ByValue;
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int rawValue)
        {
            if (rawValue != (int)EfCoreStorage.ByValue && rawValue != (int)EfCoreStorage.ByName)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.UnknownStorageType,
                    location,
                    className,
                    rawValue));

                return new EfCoreInfo(
                    Namespace: null,
                    ClassName: className,
                    FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    ValueTypeFullyQualified: string.Empty,
                    ValueTypeIsReferenceType: false,
                    ContainingTypeSimpleNames: EquatableArray<string>.Empty,
                    ContainingTypeDeclarations: EquatableArray<string>.Empty,
                    Storage: EfCoreStorage.ByValue,
                    Diagnostics: diagnostics.ToEquatableArray(),
                    Location: location);
            }

            storage = (EfCoreStorage)rawValue;
        }

        var valueTypeSymbol = baseType.TypeArguments[1];

        return new EfCoreInfo(
            Namespace: GetNamespace(classSymbol),
            ClassName: className,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValueTypeFullyQualified: valueTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValueTypeIsReferenceType: valueTypeSymbol.IsReferenceType,
            ContainingTypeSimpleNames: GetContainingTypeSimpleNames(classSymbol),
            ContainingTypeDeclarations: GetContainingTypeDeclarations(classSymbol),
            Storage: storage,
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

    private static INamedTypeSymbol? FindGenericContainingType(INamedTypeSymbol symbol)
    {
        var current = symbol.ContainingType;
        while (current is not null)
        {
            if (current.TypeParameters.Length > 0)
                return current;
            current = current.ContainingType;
        }
        return null;
    }

    private static EquatableArray<string> GetContainingTypeSimpleNames(INamedTypeSymbol symbol)
    {
        var result = new List<string>();
        var current = symbol.ContainingType;
        while (current is not null)
        {
            result.Add(current.Name);
            current = current.ContainingType;
        }
        result.Reverse();
        return result.ToEquatableArray();
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
            var staticModifier = current.IsStatic ? "static " : "";
            var nameWithTypeParams = current.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            result.Add($"partial {staticModifier}{keyword} {nameWithTypeParams}");
            current = current.ContainingType;
        }
        result.Reverse();
        return result.ToEquatableArray();
    }

    private static string? GetNamespace(INamedTypeSymbol symbol) =>
        symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();
}
