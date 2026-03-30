# Defining Enums

## Minimal Definition

The minimum required shape is:

1. A `partial` class
2. Inheriting from `OptimizedEnum<TEnum, TValue>` (or `OptimizedEnum<TEnum>`)
3. At least one `public static readonly` field of the same type

```csharp
using LayeredCraft.OptimizedEnums;

public partial class Direction : OptimizedEnum<Direction, int>
{
    public static readonly Direction North = new(1, nameof(North));
    public static readonly Direction South = new(2, nameof(South));
    public static readonly Direction East  = new(3, nameof(East));
    public static readonly Direction West  = new(4, nameof(West));

    private Direction(int value, string name) : base(value, name) { }
}
```

## Using the `int` Shorthand

For integer-valued enums, inherit from `OptimizedEnum<TEnum>` to drop the second type parameter:

```csharp
public partial class Priority : OptimizedEnum<Priority>
{
    public static readonly Priority Low    = new(1, nameof(Low));
    public static readonly Priority Medium = new(2, nameof(Medium));
    public static readonly Priority High   = new(3, nameof(High));

    private Priority(int value, string name) : base(value, name) { }
}
```

## Constructor Visibility

The constructor should be `private` to prevent direct instantiation outside the class. The generator emits a warning (OE0101) if any non-private constructor is found, but still generates the lookup code.

```csharp
// Recommended
private OrderStatus(int value, string name) : base(value, name) { }

// Allowed but triggers OE0101 warning
public OrderStatus(int value, string name) : base(value, name) { }
```

## Member Field Requirements

The generator only picks up fields that are:

- `public`
- `static`
- `readonly`
- Of the same type as the enclosing class

Non-readonly public static fields of the enum type trigger a warning (OE0102) and are excluded from generation.

```csharp
public static readonly OrderStatus Pending = new(1, nameof(Pending));  // included
public static          OrderStatus Draft   = new(0, nameof(Draft));    // OE0102, excluded
private static readonly OrderStatus Hidden = new(99, nameof(Hidden));  // excluded (private)
```

## Duplicate Detection

The generator performs best-effort duplicate detection at compile time:

- **OE0005** — duplicate values (detected when constructor arguments are compile-time constants)
- **OE0006** — duplicate member names

```csharp
// OE0005: value 1 used twice
public static readonly OrderStatus Pending = new(1, nameof(Pending));
public static readonly OrderStatus Draft   = new(1, nameof(Draft));   // error

// OE0006: name "Pending" used twice
public static readonly OrderStatus Pending  = new(1, nameof(Pending));
public static readonly OrderStatus Pending2 = new(2, "Pending");      // error
```

## Namespaces

Enums can be in any namespace, including the global namespace. The generator respects the containing namespace when emitting the partial class.
