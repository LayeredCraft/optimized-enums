using LayeredCraft.OptimizedEnums.Generator.Diagnostics;

namespace LayeredCraft.OptimizedEnums.Generator.Models;

/// <summary>
/// Immutable model representing a validated [OptimizedEnum] class discovered during
/// the incremental generator transform step.
/// </summary>
internal sealed record EnumInfo(
    string? Namespace,
    string ClassName,
    string FullyQualifiedClassName,
    string ValueTypeFullyQualified,
    EquatableArray<string> MemberNames,
    EquatableArray<string> ContainingTypeNames,
    EquatableArray<DiagnosticInfo> Diagnostics,
    LocationInfo? Location
)
{
    public bool Equals(EnumInfo? other) =>
        other is not null
        && Namespace == other.Namespace
        && ClassName == other.ClassName
        && FullyQualifiedClassName == other.FullyQualifiedClassName
        && ValueTypeFullyQualified == other.ValueTypeFullyQualified
        && MemberNames == other.MemberNames
        && ContainingTypeNames == other.ContainingTypeNames
        && Diagnostics == other.Diagnostics;

    public override int GetHashCode() =>
        HashCode.Combine(Namespace, ClassName, FullyQualifiedClassName, ValueTypeFullyQualified,
            MemberNames, ContainingTypeNames, Diagnostics);
}
