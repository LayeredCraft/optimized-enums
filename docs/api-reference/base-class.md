# Base Class

## `OptimizedEnum<TEnum, TValue>`

```csharp
public abstract partial class OptimizedEnum<TEnum, TValue>
    where TEnum  : OptimizedEnum<TEnum, TValue>
    where TValue : notnull, IComparable<TValue>
```

The primary base class. Implements `IEquatable<TEnum>`, `IComparable`, and `IComparable<TEnum>`.

### Constructor

```csharp
protected OptimizedEnum(TValue value, string name)
```

| Parameter | Description |
|-----------|-------------|
| `value` | The underlying value for this member. Must be unique across all members of the type. |
| `name`  | The display name for this member. Typically `nameof(MemberName)`. Must be unique across all members. |

Throws `ArgumentNullException` if `name` is `null`.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value`  | `TValue` | The underlying value passed to the constructor. |
| `Name`   | `string` | The name passed to the constructor. |

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ToString()` | `string` | Returns `Name`. |
| `Equals(object?)` | `bool` | `sealed`. True if `obj` is `TEnum` with the same `Value` and runtime type. |
| `Equals(TEnum?)` | `bool` | True if `other` is non-null, same runtime type, and same `Value`. |
| `GetHashCode()` | `int` | `sealed`. Hash of runtime type and `Value`. |
| `CompareTo(object?)` | `int` | Delegates to `CompareTo(TEnum?)`. Throws `ArgumentException` for wrong type. |
| `CompareTo(TEnum?)` | `int` | Compares by `Value.CompareTo`. Null sorts first. |

### Operators

| Operator | Description |
|----------|-------------|
| `==` | Delegates to `Equals`. |
| `!=` | Delegates to `!Equals`. |

---

## `OptimizedEnum<TEnum>`

```csharp
public abstract class OptimizedEnum<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
```

Convenience base class that fixes `TValue = int`. All members are inherited from `OptimizedEnum<TEnum, int>`.

### Constructor

```csharp
protected OptimizedEnum(int value, string name)
```

Identical to `OptimizedEnum<TEnum, int>(value, name)`.
