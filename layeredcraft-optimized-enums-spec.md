# LayeredCraft.OptimizedEnums – Implementation Specification

## Overview

Build a .NET library that provides a high-performance alternative to traditional Enumeration/SmartEnum patterns using **source generation**.

### Key goals

- No reflection
- AOT / trimming friendly
- Excellent developer experience
- Strong compile-time validation
- Minimal runtime overhead

---

## Core Design

### Base Type

```csharp
#nullable enable

namespace LayeredCraft.OptimizedEnums;

public abstract partial class OptimizedEnum<TEnum, TValue> :
    IEquatable<TEnum>,
    IComparable,
    IComparable<TEnum>
    where TEnum : OptimizedEnum<TEnum, TValue>
    where TValue : notnull, IComparable<TValue>
{
    public string Name { get; }
    public TValue Value { get; }

    protected OptimizedEnum(TValue value, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Value = value;
        Name = name;
    }

    public override string ToString() => Name;

    public sealed override bool Equals(object? obj) =>
        obj is TEnum other && Equals(other);

    public bool Equals(TEnum? other) =>
        other is not null &&
        GetType() == other.GetType() &&
        EqualityComparer<TValue>.Default.Equals(Value, other.Value);

    public sealed override int GetHashCode() =>
        HashCode.Combine(GetType(), Value);

    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is not TEnum other)
            throw new ArgumentException($"Object must be of type {typeof(TEnum).FullName}.", nameof(obj));

        return CompareTo(other);
    }

    public int CompareTo(TEnum? other)
    {
        if (other is null)
            return 1;

        return Value.CompareTo(other.Value);
    }

    public static bool operator ==(OptimizedEnum<TEnum, TValue>? left, OptimizedEnum<TEnum, TValue>? right) =>
        Equals(left, right);

    public static bool operator !=(OptimizedEnum<TEnum, TValue>? left, OptimizedEnum<TEnum, TValue>? right) =>
        !Equals(left, right);
}
```

---

## Attribute

Create an attribute used to opt-in to source generation:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class OptimizedEnumAttribute : Attribute
{
}
```

---

## Consumer Usage Pattern

```csharp
[OptimizedEnum]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name)
        : base(value, name)
    {
    }
}
```

---

## Source Generator Responsibilities

### Discovery

The generator must:

- Find all classes with `[OptimizedEnum]`
- Ensure they inherit from `OptimizedEnum<TEnum, TValue>`
- Ensure they are:
  - `partial`
  - `sealed`

### Field Detection

Identify all:

- `public static readonly` fields
- Of the same type as the containing class

---

## Generated Code (per enum type)

### Static Fields

```csharp
private static readonly OrderStatus[] s_all = new[] { Pending, Paid, Shipped };

private static readonly string[] s_names = new[]
{
    Pending.Name,
    Paid.Name,
    Shipped.Name
};

private static readonly int[] s_values = new[]
{
    Pending.Value,
    Paid.Value,
    Shipped.Value
};

private static readonly Dictionary<string, OrderStatus> s_byName =
    new(StringComparer.Ordinal)
    {
        [Pending.Name] = Pending,
        [Paid.Name] = Paid,
        [Shipped.Name] = Shipped
    };

private static readonly Dictionary<int, OrderStatus> s_byValue =
    new()
    {
        [Pending.Value] = Pending,
        [Paid.Value] = Paid,
        [Shipped.Value] = Shipped
    };
```

---

### Public API

```csharp
public static IReadOnlyList<OrderStatus> All => s_all;
public static IReadOnlyList<string> Names => s_names;
public static IReadOnlyList<int> Values => s_values;

public static int Count => s_all.Length;

public static OrderStatus FromName(string name)
{
    if (!s_byName.TryGetValue(name, out var result))
        throw new KeyNotFoundException($"'{name}' is not a valid name for {nameof(OrderStatus)}");

    return result;
}

public static bool TryFromName(string name, out OrderStatus? result) =>
    s_byName.TryGetValue(name, out result);

public static OrderStatus FromValue(int value)
{
    if (!s_byValue.TryGetValue(value, out var result))
        throw new KeyNotFoundException($"'{value}' is not a valid value for {nameof(OrderStatus)}");

    return result;
}

public static bool TryFromValue(int value, out OrderStatus? result) =>
    s_byValue.TryGetValue(value, out result);

public static bool ContainsName(string name) => s_byName.ContainsKey(name);
public static bool ContainsValue(int value) => s_byValue.ContainsKey(value);
```

---

## Diagnostics (Compile-Time)

The generator must emit diagnostics for:

### Errors

- Type is not `partial`
- Type is not `sealed`
- Type does not inherit from `OptimizedEnum<,>`
- No valid enum fields found
- Duplicate values
- Duplicate names

### Warnings (optional)

- Non-private constructors
- Non-readonly fields

---

## Design Rules

- No reflection anywhere
- No LINQ in generated code
- All lookups must be O(1)
- All collections must be cached (no allocations per call)
- Use `StringComparer.Ordinal` for name lookups
- Generated members must feel native to the type

---

## Explicit Non-Goals (v1)

Do NOT include:

- Implicit conversion operators
- Reflection fallback
- Localization support
- EF Core integration
- ASP.NET integration
- JSON converters (separate package)

---

## Package Structure

```
src/
  LayeredCraft.OptimizedEnums/
  LayeredCraft.OptimizedEnums.Generator/

tests/
  LayeredCraft.OptimizedEnums.Tests/
  LayeredCraft.OptimizedEnums.Generator.Tests/
  LayeredCraft.OptimizedEnums.Benchmarks/
```

---

## Future Extensions (Separate Packages)

- LayeredCraft.OptimizedEnums.Json
- LayeredCraft.OptimizedEnums.EFCore
- LayeredCraft.OptimizedEnums.AspNetCore

---

## Key Differentiators

- Source-generated lookup tables
- Zero reflection
- AOT-safe
- Better performance than SmartEnum
- Compile-time validation instead of runtime errors
