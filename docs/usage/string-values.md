# String Values

## Overview

Any value type implementing `IComparable<T>` and `notnull` works as `TValue`. `string` is a common choice when you need enum values that are meaningful in serialization or external systems.

## Defining a String-Valued Enum

```csharp
using LayeredCraft.OptimizedEnums;

public sealed partial class Color : OptimizedEnum<Color, string>
{
    public static readonly Color Red   = new("red",   nameof(Red));
    public static readonly Color Green = new("green", nameof(Green));
    public static readonly Color Blue  = new("blue",  nameof(Blue));

    private Color(string value, string name) : base(value, name) { }
}
```

## Using It

```csharp
// Lookup by value (the string)
Color color = Color.FromValue("red");     // Color.Red

// Lookup by name (the C# identifier)
Color color = Color.FromName("Red");      // Color.Red

// Note: name and value are different!
color.Name  // "Red"
color.Value // "red"
```

!!! tip "Name vs. Value"
    `Name` is always the C# identifier used in the declaration (e.g., `nameof(Red)` → `"Red"`). `Value` is whatever you pass as the first constructor argument (e.g., `"red"`). They can differ — and often should for string-valued enums where you want different casing or format in serialized output.

## Value Lookup Uses Equality

Value lookups for `string` use the default `EqualityComparer<string>`, which is ordinal/case-sensitive. If you need case-insensitive value lookups, consider normalizing the value at construction time.

## Comparison

The base class `CompareTo` delegates to `TValue.CompareTo`. For `string`, this means lexicographic comparison using the default string comparer.

## Other Value Types

Any comparable non-null type works:

```csharp
// long values
public sealed partial class EventCode : OptimizedEnum<EventCode, long>
{
    public static readonly EventCode Login  = new(1001L, nameof(Login));
    public static readonly EventCode Logout = new(1002L, nameof(Logout));

    private EventCode(long value, string name) : base(value, name) { }
}
```
