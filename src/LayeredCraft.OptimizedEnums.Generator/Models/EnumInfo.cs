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
    EquatableArray<IndexedPropertyInfo> IndexedProperties,
    LocationInfo? Location
)
{
    // Location is intentionally excluded from equality so that a position-only change
    // (e.g. the user adds a blank line above the class) does not bust the incremental
    // cache and trigger unnecessary re-emission of the same generated file.
    public bool Equals(EnumInfo? other) =>
        other is not null
        && Namespace == other.Namespace
        && ClassName == other.ClassName
        && FullyQualifiedClassName == other.FullyQualifiedClassName
        && ValueTypeFullyQualified == other.ValueTypeFullyQualified
        && MemberNames == other.MemberNames
        && ContainingTypeNames == other.ContainingTypeNames
        && Diagnostics == other.Diagnostics
        && IndexedProperties == other.IndexedProperties;

    public override int GetHashCode() =>
        HashCode.Combine(Namespace, ClassName, FullyQualifiedClassName, ValueTypeFullyQualified,
            MemberNames, ContainingTypeNames, Diagnostics, IndexedProperties);
}
