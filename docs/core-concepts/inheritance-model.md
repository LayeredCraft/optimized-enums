# Inheritance Model

## Base Classes

The library provides two base classes:

### `OptimizedEnum<TEnum, TValue>`

The primary base class. Accepts any value type that implements `IComparable<TValue>`.

```csharp
public abstract partial class OptimizedEnum<TEnum, TValue>
    where TEnum  : OptimizedEnum<TEnum, TValue>
    where TValue : notnull, IComparable<TValue>
```

Use this when your enum uses a non-`int` value type (e.g., `string`, `Guid`, `long`).

### `OptimizedEnum<TEnum>`

A convenience subclass that fixes `TValue = int`.

```csharp
public abstract class OptimizedEnum<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
```

Use this for the common case of integer-valued enums to reduce boilerplate.

## Multi-Level Inheritance

The generator walks the full inheritance chain when looking for `OptimizedEnum<TEnum, TValue>`. This means you can introduce an intermediate abstract base class:

```csharp
// Intermediate abstract base — adds domain behavior
public abstract partial class GameStatus<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
{
    public bool IsTerminal { get; }

    protected GameStatus(int value, string name, bool isTerminal)
        : base(value, name)
    {
        IsTerminal = isTerminal;
    }
}

// Concrete enum — generator fires here
public partial class DuelStatus : GameStatus<DuelStatus>
{
    public static readonly DuelStatus Active  = new(1, nameof(Active),  false);
    public static readonly DuelStatus Won     = new(2, nameof(Won),     true);
    public static readonly DuelStatus Lost    = new(3, nameof(Lost),    true);

    private DuelStatus(int value, string name, bool isTerminal)
        : base(value, name, isTerminal) { }
}
```

The generator will produce the full lookup API for `DuelStatus` because it finds `OptimizedEnum<TEnum, int>` further up the chain.

## `sealed` Is Optional

Unlike some SmartEnum libraries, `sealed` is not required. You may leave a type unsealed if you need further subclassing. The generator does not enforce or require `sealed`.

## The `partial` Requirement

`partial` is the only hard requirement. The generator needs to emit a second partial declaration for the same class. Without `partial`, the compiler cannot merge the two and the generator reports OE0001.

!!! warning "OE0001"
    If you forget `partial`, you will see:
    ```
    error OE0001: The class 'MyEnum' must be declared as partial for OptimizedEnum source generation
    ```
