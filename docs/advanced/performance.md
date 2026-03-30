# Performance

## Benchmark Results

All benchmarks run on Apple M3 Max, .NET 9.0.8 (Arm64 RyuJIT AdvSIMD), BenchmarkDotNet v0.14.0.

| Method        | Mean     | Error    | StdDev   | Allocated |
|-------------- |---------:|---------:|---------:|----------:|
| FromName      | 5.48 ns  | 0.096 ns | 0.089 ns | 0 B       |
| TryFromName   | 4.53 ns  | 0.064 ns | 0.057 ns | 0 B       |
| FromValue     | 2.18 ns  | 0.018 ns | 0.017 ns | 0 B       |
| TryFromValue  | 1.21 ns  | 0.015 ns | 0.011 ns | 0 B       |
| ContainsName  | 4.54 ns  | 0.045 ns | 0.042 ns | 0 B       |
| ContainsValue | 1.18 ns  | 0.011 ns | 0.010 ns | 0 B       |
| GetAll        | 0.76 ns  | 0.007 ns | 0.006 ns | 0 B       |
| GetCount      | ~0 ns    | —        | —        | 0 B       |

## Why These Numbers

**Zero allocations** — every operation reads from statically-initialized, cached collections. There is no per-call heap activity.

**`FromValue` / `ContainsValue` are faster than name lookups** — `int` dictionary keys hash and compare faster than `string` keys (which use `StringComparer.Ordinal` but still require character scanning).

**`GetAll` is a property returning a cached list reference** — essentially free after JIT inlining.

**`GetCount` measures as ~0 ns** — it is a `const int`. The JIT replaces it with the literal value at the call site. BenchmarkDotNet cannot distinguish it from an empty method.

## How Lookups Are Implemented

Name lookups use a `Dictionary<string, TEnum>` with `StringComparer.Ordinal`. Value lookups use a `Dictionary<TValue, TEnum>` with the default equality comparer. Both dictionaries are `static readonly` fields initialized once when the type is first accessed.

The generated code looks roughly like:

```csharp
private static readonly Dictionary<string, OrderStatus> s_byName =
    new(StringComparer.Ordinal)
    {
        ["Pending"] = Pending,
        ["Paid"]    = Paid,
        ["Shipped"] = Shipped,
    };

public static OrderStatus FromName(string name) =>
    s_byName.TryGetValue(name, out var result)
        ? result
        : throw new InvalidOperationException($"...");
```

## Comparison to Reflection-Based Approaches

A typical reflection-based SmartEnum implementation scans fields via `GetFields()` on first access and may allocate a new list per call if not carefully cached. This library eliminates both concerns entirely: the field list is known at compile time and all collections are allocated once at type initialization.

## Running the Benchmarks

```bash
cd tests/LayeredCraft.OptimizedEnums.Benchmarks
dotnet run -c Release -- --filter '*EnumLookupBenchmarks*'
```
