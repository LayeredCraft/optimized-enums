using LayeredCraft.OptimizedEnums.EFCore.Generator.Diagnostics;

namespace LayeredCraft.OptimizedEnums.EFCore.Generator.Models;

internal enum EfCoreStorage
{
    ByValue = 0,
    ByName = 1,
}

internal sealed record EfCoreInfo(
    string? Namespace,
    string ClassName,
    string FullyQualifiedClassName,
    string ValueTypeFullyQualified,
    bool ValueTypeIsReferenceType,
    EquatableArray<string> ContainingTypeSimpleNames,
    EquatableArray<string> ContainingTypeDeclarations,
    EfCoreStorage Storage,
    EquatableArray<DiagnosticInfo> Diagnostics,
    LocationInfo? Location
)
{
    // Location intentionally excluded from equality — position-only changes should not
    // bust the incremental cache and trigger unnecessary re-emission.
    public bool Equals(EfCoreInfo? other) =>
        other is not null
        && Namespace == other.Namespace
        && ClassName == other.ClassName
        && FullyQualifiedClassName == other.FullyQualifiedClassName
        && ValueTypeFullyQualified == other.ValueTypeFullyQualified
        && ValueTypeIsReferenceType == other.ValueTypeIsReferenceType
        && ContainingTypeSimpleNames == other.ContainingTypeSimpleNames
        && ContainingTypeDeclarations == other.ContainingTypeDeclarations
        && Storage == other.Storage
        && Diagnostics == other.Diagnostics;

    public override int GetHashCode() =>
        HashCode.Combine(
            HashCode.Combine(Namespace, ClassName, FullyQualifiedClassName, ValueTypeFullyQualified),
            HashCode.Combine(ValueTypeIsReferenceType, ContainingTypeSimpleNames, ContainingTypeDeclarations, Storage, Diagnostics));
}
