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
            info.Namespace,
            info.ClassName,
            info.FullyQualifiedClassName,
            info.ValueTypeFullyQualified,
            MemberNames = info.MemberNames.ToArray(),
        };

        var outputCode = TemplateHelper.Render("Templates.OptimizedEnum.scriban", model);
        context.AddSource($"{info.ClassName}.g.cs", outputCode);
    }
}
