#nullable enable

namespace LayeredCraft.OptimizedEnums;

/// <summary>
/// Marks a property on an intermediate OptimizedEnum base class as a lookup index.
/// The source generator will emit <c>From{PropertyName}</c> and <c>TryFrom{PropertyName}</c>
/// lookup methods backed by a pre-built dictionary on every concrete subclass.
/// </summary>
/// <remarks>
/// The property type must implement <see cref="IEquatable{T}"/>; otherwise diagnostic OE0202
/// is emitted and the index is skipped. For string properties, use
/// <see cref="StringComparison"/> to control key comparison.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class OptimizedEnumIndexAttribute : Attribute
{
    /// <summary>
    /// For <see cref="string"/> properties, specifies the comparison used when building
    /// the lookup dictionary. Defaults to <see cref="StringComparison.Ordinal"/>.
    /// Ignored for non-string property types.
    /// </summary>
    public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;
}