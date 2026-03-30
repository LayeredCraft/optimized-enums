# LayeredCraft.OptimizedEnums

A high-performance, AOT-safe alternative to SmartEnum patterns using source generation.

<div class="grid" markdown>
<div class="feature-box" markdown>
### Zero Reflection
All lookup tables are generated at compile time. No runtime type discovery, no `GetType()` scanning.
</div>
<div class="feature-box" markdown>
### O(1) Lookups
`FromName`, `FromValue`, `ContainsName`, and `ContainsValue` all resolve in constant time via statically-cached dictionaries.
</div>
<div class="feature-box" markdown>
### AOT & Trimming Safe
No reflection, no dynamic code. Works with `PublishAot`, `PublishTrimmed`, and NativeAOT targets out of the box.
</div>
<div class="feature-box" markdown>
### No Ceremony
Just inherit from `OptimizedEnum<TEnum, TValue>` and declare your members. The generator does the rest.
</div>
</div>

## Quick Example

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
var status = OrderStatus.FromName("Paid");       // OrderStatus.Paid
var status = OrderStatus.FromValue(3);           // OrderStatus.Shipped
bool exists = OrderStatus.ContainsName("Paid");  // true
IReadOnlyList<OrderStatus> all = OrderStatus.All;
```

## Performance

All operations are zero-allocation. Benchmarks run on Apple M3 Max, .NET 9.0.8.

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

## Installation

```
dotnet add package LayeredCraft.OptimizedEnums
```

See [Installation](getting-started/installation.md) for full setup instructions.
