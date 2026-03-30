using LayeredCraft.OptimizedEnums.Generator.Models;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.Generator.Diagnostics;

internal sealed record DiagnosticInfo(
    DiagnosticDescriptor DiagnosticDescriptor,
    LocationInfo? LocationInfo = null,
    params object?[] MessageArgs
)
{
    public bool Equals(DiagnosticInfo? other) =>
        other is not null
        && Equals(DiagnosticDescriptor.Id, other.DiagnosticDescriptor.Id)
        && Equals(LocationInfo, other.LocationInfo);

    public override int GetHashCode() =>
        HashCode.Combine(DiagnosticDescriptor.Id, LocationInfo);
}

internal static class DiagnosticInfoExtensions
{
    extension(DiagnosticInfo diagnosticInfo)
    {
        internal Diagnostic ToDiagnostic() =>
            Diagnostic.Create(
                diagnosticInfo.DiagnosticDescriptor,
                diagnosticInfo.LocationInfo?.ToLocation(),
                diagnosticInfo.MessageArgs);

        internal void ReportDiagnostic(SourceProductionContext context) =>
            context.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
    }
}
