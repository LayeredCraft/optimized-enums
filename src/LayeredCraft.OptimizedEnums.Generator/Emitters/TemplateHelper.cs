using System.Collections.Concurrent;
using System.Reflection;
using Scriban;

namespace LayeredCraft.OptimizedEnums.Generator.Emitters;

/// <summary>
/// Helper class for loading, caching, and rendering Scriban templates from embedded resources.
/// </summary>
internal static class TemplateHelper
{
    private static readonly ConcurrentDictionary<string, Template> Cache = new();

    /// <summary>
    /// Renders a Scriban template with the provided model. Templates are cached after first load.
    /// </summary>
    /// <typeparam name="TModel">The type of the model to render.</typeparam>
    /// <param name="resourceName">Relative path to the template (e.g., "Templates.OptimizedEnum.scriban")</param>
    /// <param name="model">The model object to render with the template.</param>
    /// <returns>The rendered template as a string.</returns>
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
            .FirstOrDefault(x => x.EndsWith(templateName, StringComparison.InvariantCulture));

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
