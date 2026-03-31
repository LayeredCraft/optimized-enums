using System.Collections;
using System.Collections.Immutable;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Models;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array) => _array = array;

    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    public int Length => _array.Length;

    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other) => _array.SequenceEqual(other._array);

    public override bool Equals(object? obj) =>
        obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _array)
            hash.Add(item);
        return hash.ToHashCode();
    }

    public T[] ToArray() => _array.IsDefaultOrEmpty ? Array.Empty<T>() : _array.ToArray();

    public ImmutableArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_array).GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

internal static class EquatableArrayExtensions
{
    internal static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source)
        where T : IEquatable<T> =>
        new(source.ToImmutableArray());
}
