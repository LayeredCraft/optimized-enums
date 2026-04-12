using System.Collections.Immutable;
using System.Reflection;
using LayeredCraft.OptimizedEnums.EFCore.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.EFCore.Generator.Models;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.EFCore.Generator.Emitters;

internal static class EfCoreEmitter
{
    private static string GeneratedCodeAttribute { get; } = BuildGeneratedCodeAttribute();

    private static string BuildGeneratedCodeAttribute()
    {
        var asm = Assembly.GetExecutingAssembly();
        return $"""[global::System.CodeDom.Compiler.GeneratedCode("{asm.GetName().Name}", "{asm.GetName().Version}")]""";
    }

    internal static void GeneratePerEnum(SourceProductionContext context, EfCoreInfo info)
    {
        var converterPrefix = BuildConverterPrefix(info);
        var extensionClassName = BuildExtensionClassName(info);
        var hintName = info.FullyQualifiedClassName.Replace("global::", "") + ".EFCore.g.cs";
        var namespaceLine = info.Namespace is not null ? $"namespace {info.Namespace};" : string.Empty;

        // Fully-qualified converter names for use in extension methods and conventions
        var converterFq = BuildFullyQualifiedTypeName(info, converterPrefix + "ValueConverter");
        var nameConverterFq = BuildFullyQualifiedTypeName(info, converterPrefix + "NameConverter");

        var model = new
        {
            GeneratedCodeAttribute,
            NamespaceLine = namespaceLine,
            ConverterPrefix = converterPrefix,
            ExtensionClassName = extensionClassName,
            info.ClassName,
            info.FullyQualifiedClassName,
            info.ValueTypeFullyQualified,
            ConverterFq = converterFq,
            NameConverterFq = nameConverterFq,
        };

        try
        {
            var source = TemplateHelper.Render("Templates.OptimizedEnumEfCore.scriban", model);
            context.AddSource(hintName, source);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.GeneratorInternalError,
                info.Location?.ToLocation(),
                info.ClassName,
                ex.Message));
        }
    }

    internal static void GenerateConventions(
        SourceProductionContext context,
        ImmutableArray<EfCoreInfo> infos)
    {
        var enumEntries = infos
            .Where(i => !i.Diagnostics.Any(d => d.DiagnosticDescriptor.DefaultSeverity == DiagnosticSeverity.Error))
            .Select(i =>
            {
                var converterPrefix = BuildConverterPrefix(i);
                var isByName = i.Storage == EfCoreStorage.ByName;
                return new
                {
                    FullyQualifiedClassName = i.FullyQualifiedClassName,
                    ConverterFq = BuildFullyQualifiedTypeName(i, converterPrefix + (isByName ? "NameConverter" : "ValueConverter")),
                };
            })
            .ToArray();

        var model = new
        {
            GeneratedCodeAttribute,
            Enums = enumEntries,
        };

        try
        {
            var source = TemplateHelper.Render("Templates.OptimizedEnumEfCoreConventions.scriban", model);
            context.AddSource("OptimizedEnumEfCoreConventions.g.cs", source);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.GeneratorInternalError,
                null,
                "conventions",
                ex.Message));
        }
    }

    private static string BuildConverterPrefix(EfCoreInfo info)
    {
        // For nested types: join containing type names + class name to avoid collisions
        // e.g. Outer.Status -> "OuterStatus"
        if (info.ContainingTypeSimpleNames.Length == 0)
            return info.ClassName;

        return string.Join("_", info.ContainingTypeSimpleNames) + "_" + info.ClassName;
    }

    private static string BuildExtensionClassName(EfCoreInfo info)
    {
        // Namespace segments + containing type names + class name, joined with _
        // e.g. MyApp.Domain.OrderStatus -> "MyApp_Domain_OrderStatusEfCoreExtensions"
        // e.g. MyApp.Domain.Outer.Status -> "MyApp_Domain_Outer_StatusEfCoreExtensions"
        // e.g. Priority (global ns) -> "PriorityEfCoreExtensions"
        var parts = new List<string>();
        if (info.Namespace is not null)
            parts.AddRange(info.Namespace.Split('.'));
        parts.AddRange(info.ContainingTypeSimpleNames);
        parts.Add(info.ClassName);
        return string.Join("_", parts) + "EfCoreExtensions";
    }

    private static string BuildFullyQualifiedTypeName(EfCoreInfo info, string typeName)
    {
        // Generated converter/extension classes are always emitted at namespace scope.
        // e.g. global::MyApp.Domain.OrderStatusValueConverter
        // e.g. global::MyApp.Domain.Outer_StatusValueConverter  (nested enum — still namespace-scoped)
        if (info.Namespace is null)
            return $"global::{typeName}";
        return $"global::{info.Namespace}.{typeName}";
    }
}
