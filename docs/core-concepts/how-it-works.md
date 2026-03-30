# How It Works

## The Problem with SmartEnum

The classic SmartEnum pattern provides rich enum-like types with value semantics and named members. However, traditional implementations rely on reflection to discover members at runtime:

```csharp
// Traditional approach — reflection at runtime
var members = typeof(OrderStatus)
    .GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(OrderStatus))
    .Select(f => (OrderStatus)f.GetValue(null)!)
    .ToList();
```

This has several drawbacks:

- Allocates on every call unless cached manually
- Breaks under AOT compilation and aggressive trimming
- Slow on cold paths (first access per type)

## The Source Generator Approach

`LayeredCraft.OptimizedEnums` inverts this model. At compile time, the Roslyn source generator inspects your class declaration and emits a second `partial` class file containing:

1. **Static lookup dictionaries** — `Dictionary<string, TEnum>` keyed by name, `Dictionary<TValue, TEnum>` keyed by value
2. **Static list properties** — `IReadOnlyList<TEnum>`, `IReadOnlyList<string>`, `IReadOnlyList<TValue>`
3. **Factory methods** — `FromName`, `FromValue`, `TryFromName`, `TryFromValue`
4. **Membership methods** — `ContainsName`, `ContainsValue`
5. **Count constant** — a compile-time `int` constant

Because the generator reads your source directly, no reflection is ever needed at runtime.

## Generator Pipeline

```
Your source file
      │
      ▼
 Roslyn compiler triggers IIncrementalGenerator
      │
      ▼
 Syntax predicate: ClassDeclarationSyntax { BaseList: not null }
      │
      ▼
 Semantic transform: inherits OptimizedEnum<TEnum,TValue>?
      │
      ▼
 EnumInfo model built (members, value type, namespace, diagnostics)
      │
      ▼
 Scriban template rendered → partial class source
      │
      ▼
 Emitted into compilation
```

The pipeline is incremental — Roslyn caches the `EnumInfo` model between builds. If you only change unrelated files, the generator does not re-run.

## What Gets Generated

Given this input:

```csharp
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

The generator emits a file roughly equivalent to:

```csharp
partial class OrderStatus
{
    private static readonly Dictionary<string, OrderStatus> s_byName = new(StringComparer.Ordinal)
    {
        ["Pending"] = Pending,
        ["Paid"]    = Paid,
        ["Shipped"] = Shipped,
    };

    private static readonly Dictionary<int, OrderStatus> s_byValue = new()
    {
        [1] = Pending,
        [2] = Paid,
        [3] = Shipped,
    };

    public static IReadOnlyList<OrderStatus> All    { get; } = [Pending, Paid, Shipped];
    public static IReadOnlyList<string>      Names  { get; } = ["Pending", "Paid", "Shipped"];
    public static IReadOnlyList<int>         Values { get; } = [1, 2, 3];
    public const  int                        Count  = 3;

    public static OrderStatus FromName(string name) => ...;
    public static OrderStatus FromValue(int value)  => ...;
    // ...
}
```

See [Generated Members](../api-reference/generated-members.md) for the full API surface.
