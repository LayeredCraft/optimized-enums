using System.Reflection;
using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Models;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Emitters;

internal static class JsonConverterEmitter
{
    private static readonly string GeneratedCodeAttribute = BuildGeneratedCodeAttribute();

    private static string BuildGeneratedCodeAttribute()
    {
        var asm = Assembly.GetExecutingAssembly();
        return $"""[global::System.CodeDom.Compiler.GeneratedCode("{asm.GetName().Name}", "{asm.GetName().Version}")]""";
    }

    internal static void Generate(SourceProductionContext context, JsonConverterInfo info)
    {
        var converterSuffix = info.ConverterType == OptimizedEnumJsonConverterType.ByName ? "Name" : "Value";
        var converterClassName = $"{info.ClassName}{converterSuffix}JsonConverter";
        var hintName = info.FullyQualifiedClassName.Replace("global::", "") + ".SystemTextJson.g.cs";

        var model = new
        {
            GeneratedCodeAttribute,
            ConverterClassName = converterClassName,
            info.ClassName,
            info.FullyQualifiedClassName,
            info.ValueTypeFullyQualified,
            info.ValueTypeIsReferenceType,
            IsByName = info.ConverterType == OptimizedEnumJsonConverterType.ByName,
            Preamble = BuildPreamble(info),
            Suffix = BuildSuffix(info),
        };

        try
        {
            var source = TemplateHelper.Render("Templates.JsonConverter.scriban", model);
            context.AddSource(hintName, source);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "OE9002",
                    "OptimizedEnums SystemTextJson generator internal error",
                    "An unexpected error occurred while generating the JSON converter for '{0}': {1}",
                    "OptimizedEnums.SystemTextJson",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                info.Location?.ToLocation(),
                info.ClassName,
                ex.Message));
        }
    }

    private static string BuildPreamble(JsonConverterInfo info)
    {
        if (info.Namespace is null && info.ContainingTypeNames.Length == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        if (info.Namespace is not null)
            sb.Append("namespace ").Append(info.Namespace).Append(";\n\n");

        foreach (var ct in info.ContainingTypeNames)
            sb.Append(ct).Append("\n{\n");

        return sb.ToString();
    }

    private static string BuildSuffix(JsonConverterInfo info)
    {
        if (info.ContainingTypeNames.Length == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < info.ContainingTypeNames.Length; i++)
            sb.Append("\n}");

        return sb.ToString();
    }
}
