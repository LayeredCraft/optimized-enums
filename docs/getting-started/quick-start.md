# Quick Start

## 1. Install the Package

```bash
dotnet add package LayeredCraft.OptimizedEnums
```

## 2. Define Your Enum

Inherit from `OptimizedEnum<TEnum, TValue>`, declare the class `partial`, and add your members as `public static readonly` fields of the same type.

```csharp
using LayeredCraft.OptimizedEnums;

public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

!!! note "Why `partial`?"
    The source generator augments your class with a second partial declaration containing the generated lookup members. Without `partial`, the compiler cannot merge the two declarations and the generator emits a build error (OE0001).

## 3. Use the Generated API

```csharp
// Lookup by name
OrderStatus status = OrderStatus.FromName("Paid");

// Lookup by value
OrderStatus status = OrderStatus.FromValue(2);

// Try-style (no exception on miss)
if (OrderStatus.TryFromName("Unknown", out var result))
    Console.WriteLine(result.Name);

// Membership checks
bool exists = OrderStatus.ContainsName("Shipped");   // true
bool exists = OrderStatus.ContainsValue(99);          // false

// Enumeration
IReadOnlyList<OrderStatus> all    = OrderStatus.All;
IReadOnlyList<string>      names  = OrderStatus.Names;
IReadOnlyList<int>         values = OrderStatus.Values;
int count = OrderStatus.Count;
```

## 4. Using `int` as the Default Value Type

For `int`-valued enums you can use the single-parameter convenience base class:

```csharp
public sealed partial class Priority : OptimizedEnum<Priority>
{
    public static readonly Priority Low    = new(1, nameof(Low));
    public static readonly Priority Medium = new(2, nameof(Medium));
    public static readonly Priority High   = new(3, nameof(High));

    private Priority(int value, string name) : base(value, name) { }
}
```

## 5. String Values

Any value type implementing `IComparable<T>` works, including `string`:

```csharp
public sealed partial class Color : OptimizedEnum<Color, string>
{
    public static readonly Color Red   = new("red",   nameof(Red));
    public static readonly Color Green = new("green", nameof(Green));
    public static readonly Color Blue  = new("blue",  nameof(Blue));

    private Color(string value, string name) : base(value, name) { }
}
```

## 6. JSON Serialization

To serialize/deserialize your enum with `System.Text.Json`, install the SystemTextJson package (it pulls in the core package automatically):

```bash
dotnet add package LayeredCraft.OptimizedEnums.SystemTextJson
```

Then decorate your class with `[OptimizedEnumJsonConverter]`:

```csharp
using LayeredCraft.OptimizedEnums.SystemTextJson;

[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

`OrderStatus` now serializes as `"Pending"` / `"Paid"` / `"Shipped"` with no manual converter registration. See [JSON Serialization](../usage/json-serialization.md) for full details on `ByName` vs `ByValue` and AOT safety.

## Next Steps

- [Core Concepts — How It Works](../core-concepts/how-it-works.md)
- [Usage — Defining Enums](../usage/defining-enums.md)
- [Usage — JSON Serialization](../usage/json-serialization.md)
- [API Reference — Generated Members](../api-reference/generated-members.md)
