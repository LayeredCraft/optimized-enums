using LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Diagnostics;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Models;

internal sealed record JsonConverterInfo(
    string? Namespace,
    string ClassName,
    string FullyQualifiedClassName,
    string ValueTypeFullyQualified,
    bool ValueTypeIsReferenceType,
    EquatableArray<string> ContainingTypeNames,
    OptimizedEnumJsonConverterType ConverterType,
    EquatableArray<DiagnosticInfo> Diagnostics,
    LocationInfo? Location
)
{
    // Location intentionally excluded from equality — position-only changes should not
    // bust the incremental cache and trigger unnecessary re-emission.
    public bool Equals(JsonConverterInfo? other) =>
        other is not null
        && Namespace == other.Namespace
        && ClassName == other.ClassName
        && FullyQualifiedClassName == other.FullyQualifiedClassName
        && ValueTypeFullyQualified == other.ValueTypeFullyQualified
        && ValueTypeIsReferenceType == other.ValueTypeIsReferenceType
        && ContainingTypeNames == other.ContainingTypeNames
        && ConverterType == other.ConverterType
        && Diagnostics == other.Diagnostics;

    public override int GetHashCode() =>
        HashCode.Combine(Namespace, ClassName, FullyQualifiedClassName, ValueTypeFullyQualified,
            ValueTypeIsReferenceType, ContainingTypeNames, ConverterType, Diagnostics);
}
