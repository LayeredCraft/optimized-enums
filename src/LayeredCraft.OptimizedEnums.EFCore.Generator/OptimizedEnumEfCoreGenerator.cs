using LayeredCraft.OptimizedEnums.EFCore.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.EFCore.Generator.Emitters;
using LayeredCraft.OptimizedEnums.EFCore.Generator.Providers;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.EFCore.Generator;

/// <summary>Source generator that emits EF Core value converters for OptimizedEnum types.</summary>
[Generator]
public sealed class OptimizedEnumEfCoreGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource(AttributeSource.HintName, AttributeSource.Source));

        var efCoreInfos = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                EfCoreSyntaxProvider.AttributeMetadataName,
                EfCoreSyntaxProvider.Predicate,
                EfCoreSyntaxProvider.Transform)
            .WithTrackingName(TrackingNames.EfCoreSyntaxProvider_Extract)
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName(TrackingNames.EfCoreSyntaxProvider_FilterNotNull);

        // Per-enum: emit converters, comparer, and enum-specific extension methods
        context.RegisterSourceOutput(efCoreInfos, static (ctx, info) =>
        {
            foreach (var diagnostic in info.Diagnostics)
                diagnostic.ReportDiagnostic(ctx);

            if (info.Diagnostics.Any(d => d.DiagnosticDescriptor.DefaultSeverity == DiagnosticSeverity.Error))
                return;

            EfCoreEmitter.GeneratePerEnum(ctx, info);
        });

        // Shared: emit the ConfigureOptimizedEnums() convention hook once per compilation,
        // collecting all valid opted-in enums.
        var collected = efCoreInfos
            .Collect()
            .WithTrackingName(TrackingNames.EfCoreSyntaxProvider_Collect);

        context.RegisterSourceOutput(collected, static (ctx, infos) =>
            EfCoreEmitter.GenerateConventions(ctx, infos));
    }
}
