using System.Reflection;
using LayeredCraft.OptimizedEnums.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.Generator.Models;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.Generator.Emitters;

internal static class EnumEmitter
{
    private static string GeneratedCodeAttribute
    {
        get
        {
            if (field is null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var name = assembly.GetName().Name;
                var version = assembly.GetName().Version!.ToString();
                field = $"""[global::System.CodeDom.Compiler.GeneratedCode("{name}", "{version}")]""";
            }

            return field;
        }
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

        var outputCode = TemplateHelper.Render("Templates.OptimizedEnum.scriban", model);
        // Use the fully-qualified name (minus "global::") as the hint name to avoid
        // collisions when two types share a class name in different namespaces.
        var hintName = info.FullyQualifiedClassName.Replace("global::", "") + ".g.cs";
        context.AddSource(hintName, outputCode);
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
