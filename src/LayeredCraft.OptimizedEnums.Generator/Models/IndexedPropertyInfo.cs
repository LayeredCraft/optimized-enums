namespace LayeredCraft.OptimizedEnums.Generator.Models;

/// <summary>
/// Represents a property decorated with <c>[OptimizedEnumIndex]</c> that the generator
/// should emit a pre-built dictionary lookup for on the concrete enum class.
/// </summary>
/// <param name="PropertyName">The property name as declared (e.g. "SlotValue").</param>
/// <param name="PropertyTypeFullyQualified">The fully-qualified type of the property.</param>
/// <param name="IsStringType">True when the property type is <see cref="string"/>.</param>
/// <param name="StringComparerExpression">
/// The <c>StringComparer</c> expression to pass to the dictionary constructor
/// (e.g. "global::System.StringComparer.Ordinal"). Empty for non-string types.
/// </param>
internal sealed record IndexedPropertyInfo(
    string PropertyName,
    string PropertyTypeFullyQualified,
    bool IsStringType,
    string StringComparerExpression
) : IEquatable<IndexedPropertyInfo>;