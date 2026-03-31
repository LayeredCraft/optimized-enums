using System.Collections.Concurrent;
using System.Reflection;
using Scriban;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Emitters;

internal static class TemplateHelper
{
    private static readonly ConcurrentDictionary<string, Template> Cache = new();

    internal static string Render<TModel>(string resourceName, TModel model)
    {
        var template = Cache.GetOrAdd(resourceName, LoadTemplate);
        return template.Render(model);
    }

    private static Template LoadTemplate(string relativePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var baseName = assembly.GetName().Name;

        var templateName = relativePath
            .TrimStart('.')
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');

        var manifestTemplateName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(templateName, StringComparison.Ordinal));

        if (string.IsNullOrEmpty(manifestTemplateName))
        {
            var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Did not find required resource ending in '{templateName}' in assembly '{baseName}'. "
                + $"Available resources: {availableResources}");
        }

        using var stream = assembly.GetManifestResourceStream(manifestTemplateName);
        if (stream == null)
            throw new FileNotFoundException(
                $"Template '{relativePath}' not found in embedded resources. "
                + $"Manifest resource name: '{manifestTemplateName}'");

        using var reader = new StreamReader(stream);
        var templateContent = reader.ReadToEnd();

        var template = Template.Parse(templateContent, relativePath);
        if (!template.HasErrors)
            return template;

        var errors = string.Join(
            "\n",
            template.Messages.Select(m =>
                $"{relativePath}({m.Span.Start.Line},{m.Span.Start.Column}): {m.Message}"));

        throw new InvalidOperationException(
            $"Failed to parse template '{relativePath}':\n{errors}");
    }
}
