using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Providers;

internal static class JsonConverterSyntaxProvider
{
    internal const string AttributeMetadataName =
        "LayeredCraft.OptimizedEnums.SystemTextJson.OptimizedEnumJsonConverterAttribute";

    private const string OptimizedEnumBaseMetadataName =
        "LayeredCraft.OptimizedEnums.OptimizedEnum`2";

    internal static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax;

    internal static JsonConverterInfo? Transform(
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

        // OE2001: must inherit from OptimizedEnum<,>
        var baseType = FindOptimizedEnumBase(classSymbol, context.SemanticModel.Compilation);
        if (baseType is null)
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.MustInheritOptimizedEnum,
                location,
                className));

            return new JsonConverterInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                ValueTypeIsReferenceType: false,
                ContainingTypeNames: EquatableArray<string>.Empty,
                ConverterType: OptimizedEnumJsonConverterType.ByName,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // OE2002: must be partial
        if (!classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.MustBePartial,
                location,
                className));

            return new JsonConverterInfo(
                Namespace: null,
                ClassName: className,
                FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ValueTypeFullyQualified: string.Empty,
                ValueTypeIsReferenceType: false,
                ContainingTypeNames: EquatableArray<string>.Empty,
                ConverterType: OptimizedEnumJsonConverterType.ByName,
                Diagnostics: diagnostics.ToEquatableArray(),
                Location: location);
        }

        // Read ConverterType from the attribute constructor argument
        var converterType = OptimizedEnumJsonConverterType.ByName;
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int rawValue)
            converterType = (OptimizedEnumJsonConverterType)rawValue;

        var valueTypeSymbol = baseType.TypeArguments[1];

        return new JsonConverterInfo(
            Namespace: GetNamespace(classSymbol),
            ClassName: className,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValueTypeFullyQualified: valueTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValueTypeIsReferenceType: valueTypeSymbol.IsReferenceType,
            ContainingTypeNames: GetContainingTypeDeclarations(classSymbol),
            ConverterType: converterType,
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
