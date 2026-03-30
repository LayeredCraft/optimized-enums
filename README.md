# LayeredCraft.OptimizedEnums

**LayeredCraft.OptimizedEnums** is a modular C# .NET library providing high-performance, AOT-safe smart enum patterns using source generation. Inherit from a base class and the generator produces O(1) lookup tables, collection properties, and factory methods — all at compile time with zero reflection at runtime.

## Key Features

- **Zero reflection** — all lookup tables are source-generated at compile time
- **AOT / trimming friendly** — compatible with NativeAOT, ReadyToRun, and Blazor WASM
- **O(1) lookups** — `FromName`, `FromValue`, `ContainsName`, `ContainsValue`
- **Compile-time validation** — errors for missing `partial`, duplicate values/names
- **No allocations per call** — all collections are statically cached
- **Inheritance-based triggering** — no attribute required, just inherit and go

## 📦 Packages

| Package | NuGet | Downloads |
|---------|-------|-----------|
| **LayeredCraft.OptimizedEnums** | [![NuGet](https://img.shields.io/nuget/v/LayeredCraft.OptimizedEnums.svg)](https://www.nuget.org/packages/LayeredCraft.OptimizedEnums) | [![Downloads](https://img.shields.io/nuget/dt/LayeredCraft.OptimizedEnums.svg)](https://www.nuget.org/packages/LayeredCraft.OptimizedEnums/) |
| **LayeredCraft.OptimizedEnums.SystemTextJson** | _coming soon_ | |
| **LayeredCraft.OptimizedEnums.EFCore** | _coming soon_ | |
| **LayeredCraft.OptimizedEnums.Dapper** | _coming soon_ | |
| **LayeredCraft.OptimizedEnums.AutoFixture** | _coming soon_ | |

[![Build Status](https://github.com/LayeredCraft/optimized-enums/actions/workflows/build.yaml/badge.svg)](https://github.com/LayeredCraft/optimized-enums/actions/workflows/build.yaml)

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

Or use the `int`-defaulting convenience base class:

```csharp
public sealed partial class Priority : OptimizedEnum<Priority>
{
    public static readonly Priority Low    = new(1, nameof(Low));
    public static readonly Priority Medium = new(2, nameof(Medium));
    public static readonly Priority High   = new(3, nameof(High));

    private Priority(int value, string name) : base(value, name) { }
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
int count = OrderStatus.Count;      // compile-time constant
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

```bash
dotnet add package LayeredCraft.OptimizedEnums
```

Supports **.NET 8.0**, **.NET 9.0**, **.NET 10.0**.

## Documentation

Full documentation is available at the [LayeredCraft.OptimizedEnums docs site](https://layeredcraft.github.io/optimized-enums).

## License

MIT
