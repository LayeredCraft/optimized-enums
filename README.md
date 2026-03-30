# LayeredCraft.OptimizedEnums

A high-performance, AOT-safe alternative to SmartEnum patterns using source generation.

## Features

- **Zero reflection** — all lookup tables are source-generated at compile time
- **AOT / trimming friendly** — no runtime type discovery
- **O(1) lookups** — `FromName`, `FromValue`, `ContainsName`, `ContainsValue`
- **Compile-time validation** — errors for missing `partial`, duplicate values/names
- **No allocations per call** — all collections are statically cached

## Usage

```csharp
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

The source generator produces:

```csharp
// Lookup
var status = OrderStatus.FromName("Paid");       // OrderStatus.Paid
var status = OrderStatus.FromValue(3);           // OrderStatus.Shipped

// Try-style
OrderStatus.TryFromName("Paid", out var result);
OrderStatus.TryFromValue(3, out var result);

// Membership
OrderStatus.ContainsName("Paid");   // true
OrderStatus.ContainsValue(99);      // false

// Enumeration
IReadOnlyList<OrderStatus> all    = OrderStatus.All;
IReadOnlyList<string>      names  = OrderStatus.Names;
IReadOnlyList<int>         values = OrderStatus.Values;
int count = OrderStatus.Count;
```

## Performance

Benchmarks run on Apple M3 Max, .NET 9.0.8, BenchmarkDotNet v0.14.0.

| Method        | Mean     | Allocated |
|-------------- |---------:|----------:|
| FromName      | 5.48 ns  | 0 B       |
| TryFromName   | 4.53 ns  | 0 B       |
| FromValue     | 2.18 ns  | 0 B       |
| TryFromValue  | 1.21 ns  | 0 B       |
| ContainsName  | 4.54 ns  | 0 B       |
| ContainsValue | 1.18 ns  | 0 B       |
| GetAll        | 0.76 ns  | 0 B       |
| GetCount      | ~0 ns    | 0 B       |

All lookups are O(1) via statically-cached dictionaries. `Count` is a compile-time constant.

## Installation

```
dotnet add package LayeredCraft.OptimizedEnums
```

## License

MIT
