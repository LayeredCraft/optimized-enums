#nullable enable

namespace LayeredCraft.OptimizedEnums;

/// <summary>
/// Abstract base class for source-generated, high-performance enum types.
/// </summary>
/// <typeparam name="TEnum">The concrete enum type deriving from this class.</typeparam>
/// <typeparam name="TValue">The underlying value type for this enum.</typeparam>
public abstract partial class OptimizedEnum<TEnum, TValue> :
    IEquatable<TEnum>,
    IComparable,
    IComparable<TEnum>
    where TEnum : OptimizedEnum<TEnum, TValue>
    where TValue : notnull, IComparable<TValue>
{
    /// <summary>Gets the name of this enum member.</summary>
    public string Name { get; }

    /// <summary>Gets the underlying value of this enum member.</summary>
    public TValue Value { get; }

    /// <summary>Initializes a new enum member with the given value and name.</summary>
    protected OptimizedEnum(TValue value, string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        Value = value;
        Name = name;
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <inheritdoc />
    public sealed override bool Equals(object? obj) =>
        obj is TEnum other && Equals(other);

    /// <inheritdoc />
    public bool Equals(TEnum? other) =>
        other is not null &&
        GetType() == other.GetType() &&
        EqualityComparer<TValue>.Default.Equals(Value, other.Value);

    /// <inheritdoc />
    public sealed override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + GetType().GetHashCode();
            hash = (hash * 31) + EqualityComparer<TValue>.Default.GetHashCode(Value);
            return hash;
        }
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is not TEnum other)
            throw new ArgumentException($"Object must be of type {typeof(TEnum).FullName}.", nameof(obj));

        return CompareTo(other);
    }

    /// <inheritdoc />
    public int CompareTo(TEnum? other)
    {
        if (other is null)
            return 1;

        return Value.CompareTo(other.Value);
    }

    /// <summary>Returns true if <paramref name="left"/> equals <paramref name="right"/>.</summary>
    public static bool operator ==(OptimizedEnum<TEnum, TValue>? left, OptimizedEnum<TEnum, TValue>? right) =>
        Equals(left, right);

    /// <summary>Returns true if <paramref name="left"/> does not equal <paramref name="right"/>.</summary>
    public static bool operator !=(OptimizedEnum<TEnum, TValue>? left, OptimizedEnum<TEnum, TValue>? right) =>
        !Equals(left, right);
}

/// <summary>
/// Abstract base class for source-generated, high-performance enum types with an <see cref="int"/> value.
/// </summary>
/// <typeparam name="TEnum">The concrete enum type deriving from this class.</typeparam>
public abstract class OptimizedEnum<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
{
    /// <summary>Initializes a new enum member with the given value and name.</summary>
    protected OptimizedEnum(int value, string name) : base(value, name) { }
}