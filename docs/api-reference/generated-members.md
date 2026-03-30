# Generated Members

The source generator emits the following members into a second `partial` class declaration for each qualifying type.

## Static Properties

### `All`

```csharp
public static IReadOnlyList<TEnum> All { get; }
```

All members in declaration order. Backed by a statically-initialized array.

### `Names`

```csharp
public static IReadOnlyList<string> Names { get; }
```

All member names in declaration order.

### `Values`

```csharp
public static IReadOnlyList<TValue> Values { get; }
```

All member values in declaration order.

### `Count`

```csharp
public const int Count = <N>;
```

The number of members. A compile-time constant — replaced with the literal value by the JIT.

## Factory Methods

### `FromName`

```csharp
public static TEnum FromName(string name)
```

Returns the member with the given name. Uses `StringComparer.Ordinal` (case-sensitive).

**Throws:** `InvalidOperationException` if no member has that name.

### `TryFromName`

```csharp
public static bool TryFromName(string name, out TEnum? result)
```

Returns `true` and sets `result` if found. Returns `false` and sets `result = null` if not found. Never throws.

### `FromValue`

```csharp
public static TEnum FromValue(TValue value)
```

Returns the member with the given value.

**Throws:** `InvalidOperationException` if no member has that value.

### `TryFromValue`

```csharp
public static bool TryFromValue(TValue value, out TEnum? result)
```

Returns `true` and sets `result` if found. Returns `false` and sets `result = null` if not found. Never throws.

## Membership Methods

### `ContainsName`

```csharp
public static bool ContainsName(string name)
```

Returns `true` if a member with that name exists. Equivalent to `TryFromName(name, out _)` but marginally faster (no out parameter overhead).

### `ContainsValue`

```csharp
public static bool ContainsValue(TValue value)
```

Returns `true` if a member with that value exists.

## Implementation Notes

All lookup methods are backed by pre-built `Dictionary<K, TEnum>` instances:

- `s_byName` — `Dictionary<string, TEnum>` with `StringComparer.Ordinal`
- `s_byValue` — `Dictionary<TValue, TEnum>` with default comparer

Both are `static readonly` fields initialized at type-load time. Lookups are O(1).
