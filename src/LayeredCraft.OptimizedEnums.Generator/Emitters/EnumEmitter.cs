using System.Reflection;
using LayeredCraft.OptimizedEnums.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.Generator.Models;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.Generator.Emitters;

internal static class EnumEmitter
{
    private static readonly string GeneratedCodeAttribute = BuildGeneratedCodeAttribute();

    private static string BuildGeneratedCodeAttribute()
    {
        var asm = Assembly.GetExecutingAssembly();
        return $"""[global::System.CodeDom.Compiler.GeneratedCode("{asm.GetName().Name}", "{asm.GetName().Version}")]""";
    }

    internal static void Generate(SourceProductionContext context, EnumInfo info)
    {
        var model = new
        {
            GeneratedCodeAttribute,
            info.ClassName,
            info.FullyQualifiedClassName,
            info.ValueTypeFullyQualified,
            MemberNames = info.MemberNames.ToArray(),
            Preamble = BuildPreamble(info),
            Suffix = BuildSuffix(info),
        };

        // Use the fully-qualified name (minus "global::") as the hint name to avoid
        // collisions when two types share a class name in different namespaces.
        var hintName = info.FullyQualifiedClassName.Replace("global::", "") + ".g.cs";

        try
        {
            var outputCode = TemplateHelper.Render("Templates.OptimizedEnum.scriban", model);
            context.AddSource(hintName, outputCode);
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

    private static string BuildPreamble(EnumInfo info)
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

    private static string BuildSuffix(EnumInfo info)
    {
        if (info.ContainingTypeNames.Length == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < info.ContainingTypeNames.Length; i++)
            sb.Append("\n}");

        return sb.ToString();
    }
}
