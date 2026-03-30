# Lookups & Queries

## Lookup by Name

### `FromName(string name)`

Returns the enum member with the given name. Throws `InvalidOperationException` if not found.

```csharp
OrderStatus status = OrderStatus.FromName("Paid");
// Returns OrderStatus.Paid
```

### `TryFromName(string name, out TEnum? result)`

Returns `true` and populates `result` if found. Returns `false` and sets `result` to `null` if not found. Never throws.

```csharp
if (OrderStatus.TryFromName("Unknown", out var status))
{
    Console.WriteLine(status.Name);
}
```

Name lookups use `StringComparer.Ordinal` (case-sensitive, culture-insensitive).

## Lookup by Value

### `FromValue(TValue value)`

Returns the enum member with the given value. Throws `InvalidOperationException` if not found.

```csharp
OrderStatus status = OrderStatus.FromValue(2);
// Returns OrderStatus.Paid (if value 2 is Paid)
```

### `TryFromValue(TValue value, out TEnum? result)`

Returns `true` and populates `result` if found. Never throws.

```csharp
if (OrderStatus.TryFromValue(99, out var status))
{
    Console.WriteLine(status.Value);
}
```

## Membership Checks

### `ContainsName(string name)`

Returns `true` if a member with that name exists.

```csharp
bool exists = OrderStatus.ContainsName("Shipped");   // true
bool exists = OrderStatus.ContainsName("Cancelled"); // false
```

### `ContainsValue(TValue value)`

Returns `true` if a member with that value exists.

```csharp
bool exists = OrderStatus.ContainsValue(3);   // true
bool exists = OrderStatus.ContainsValue(99);  // false
```

## Enumeration

### `All`

Returns all members in declaration order as `IReadOnlyList<TEnum>`.

```csharp
foreach (var status in OrderStatus.All)
    Console.WriteLine($"{status.Name} = {status.Value}");
```

### `Names`

Returns all member names as `IReadOnlyList<string>`.

```csharp
IReadOnlyList<string> names = OrderStatus.Names;
// ["Pending", "Paid", "Shipped"]
```

### `Values`

Returns all member values as `IReadOnlyList<TValue>`.

```csharp
IReadOnlyList<int> values = OrderStatus.Values;
// [1, 2, 3]
```

### `Count`

A compile-time `const int` equal to the number of members.

```csharp
int n = OrderStatus.Count; // 3
```

## Base Class Members

Every enum member also inherits from the base class:

| Member | Type | Description |
|--------|------|-------------|
| `Name`  | `string` | The member's name |
| `Value` | `TValue` | The member's value |
| `ToString()` | `string` | Returns `Name` |
| `Equals(TEnum?)` | `bool` | Value equality |
| `CompareTo(TEnum?)` | `int` | Comparison via `TValue.CompareTo` |
| `==` / `!=` | `bool` | Operator overloads |
