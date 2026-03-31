using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Emitters;
using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Providers;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator;

/// <summary>Source generator that emits System.Text.Json converters for OptimizedEnum types.</summary>
[Generator]
public sealed class OptimizedEnumJsonConverterGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource(AttributeSource.HintName, AttributeSource.Source));

        var converterInfos = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                JsonConverterSyntaxProvider.AttributeMetadataName,
                JsonConverterSyntaxProvider.Predicate,
                JsonConverterSyntaxProvider.Transform)
            .WithTrackingName(TrackingNames.JsonConverterSyntaxProvider_Extract)
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName(TrackingNames.JsonConverterSyntaxProvider_FilterNotNull);

        context.RegisterSourceOutput(converterInfos, static (ctx, info) =>
        {
            foreach (var diagnostic in info.Diagnostics)
                diagnostic.ReportDiagnostic(ctx);

            if (info.Diagnostics.Any(d => d.DiagnosticDescriptor.DefaultSeverity == DiagnosticSeverity.Error))
                return;

            JsonConverterEmitter.Generate(ctx, info);
        });
    }
}
