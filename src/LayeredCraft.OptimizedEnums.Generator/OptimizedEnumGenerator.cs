using LayeredCraft.OptimizedEnums.Generator.Diagnostics;
using LayeredCraft.OptimizedEnums.Generator.Emitters;
using LayeredCraft.OptimizedEnums.Generator.Providers;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.Generator;

/// <summary>Source code generator for LayeredCraft.OptimizedEnums.</summary>
[Generator]
public sealed class OptimizedEnumGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enumInfos = context.SyntaxProvider
            .CreateSyntaxProvider(
                EnumSyntaxProvider.Predicate,
                EnumSyntaxProvider.Transform)
            .WithTrackingName(TrackingNames.EnumSyntaxProvider_Extract)
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName(TrackingNames.EnumSyntaxProvider_FilterNotNull);

        context.RegisterSourceOutput(enumInfos, static (ctx, info) =>
        {
            // Report all diagnostics first
            foreach (var diagnostic in info.Diagnostics)
                diagnostic.ReportDiagnostic(ctx);

            // Skip code generation if any error-level diagnostic was emitted
            if (info.Diagnostics.Any(diagnostic =>
                    diagnostic.DiagnosticDescriptor.DefaultSeverity == DiagnosticSeverity.Error))
            {
                return;
            }

            // Belt-and-suspenders: OE0004 should have fired if members are empty
            if (info.MemberNames.Length == 0)
                return;

            EnumEmitter.Generate(ctx, info);
        });
    }
}
