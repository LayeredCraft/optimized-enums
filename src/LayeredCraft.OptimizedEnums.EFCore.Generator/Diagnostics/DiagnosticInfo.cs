using LayeredCraft.OptimizedEnums.EFCore.Generator.Models;
using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.EFCore.Generator.Diagnostics;

internal sealed record DiagnosticInfo(
    DiagnosticDescriptor DiagnosticDescriptor,
    LocationInfo? LocationInfo = null,
    params object?[] MessageArgs
)
{
    public bool Equals(DiagnosticInfo? other) =>
        other is not null
        && DiagnosticDescriptor.Id == other.DiagnosticDescriptor.Id
        && Equals(LocationInfo, other.LocationInfo)
        && MessageArgs.SequenceEqual(other.MessageArgs);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DiagnosticDescriptor.Id);
        hash.Add(LocationInfo);
        foreach (var arg in MessageArgs)
            hash.Add(arg);
        return hash.ToHashCode();
    }
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
