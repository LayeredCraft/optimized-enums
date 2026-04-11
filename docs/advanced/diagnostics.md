# Diagnostics

The generator emits structured diagnostics with the prefix `OE`. Errors block code generation; warnings allow generation to proceed.

## Errors

### OE0001 — Must Be Partial

**Message:** `The class '{0}' must be declared as partial for OptimizedEnum source generation`

**Cause:** A class that inherits from `OptimizedEnum<TEnum, TValue>` is missing the `partial` keyword.

**Fix:**
```csharp
// Before
public sealed class OrderStatus : OptimizedEnum<OrderStatus, int> { }

// After
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { }
```

### OE0004 — No Members Found

**Message:** `The class '{0}' has no public static readonly fields of its own type`

**Cause:** The class contains no `public static readonly` fields of its own type. The generator has nothing to build a lookup table from.

**Fix:** Add at least one member:
```csharp
public static readonly OrderStatus Pending = new(1, nameof(Pending));
```

### OE0005 — Duplicate Value

**Message:** `The class '{0}' has duplicate value on fields '{1}' and '{2}'`

**Cause:** Two members share the same value. Detected best-effort when constructor arguments are compile-time constants.

**Fix:** Ensure all values are unique.

### OE0006 — Duplicate Name

**Message:** `The class '{0}' has a duplicate member name '{1}'`

**Cause:** Two fields have the same name (unusual but possible via shadowing or copy-paste).

**Fix:** Rename one of the fields.

## Warnings

### OE0101 — Non-Private Constructor

**Message:** `The class '{0}' has a non-private constructor; OptimizedEnum constructors should be private to prevent direct instantiation`

**Cause:** A constructor has non-private accessibility.

**Recommendation:** Make the constructor `private`. Generation still proceeds.

### OE0102 — Non-Readonly Field

**Message:** `The field '{0}' in class '{1}' is a public static field of the enum type but is not readonly`

**Cause:** A `public static` field of the enum type is missing `readonly`. The field is excluded from the generated lookup tables.

**Fix:** Add `readonly` to the field declaration.

## Suppressing Warnings

Warnings can be suppressed via standard MSBuild mechanisms:

```xml
<!-- In your .csproj -->
<PropertyGroup>
  <NoWarn>$(NoWarn);OE0101</NoWarn>
</PropertyGroup>
```

Or inline:

```csharp
#pragma warning disable OE0101
public OrderStatus(int value, string name) : base(value, name) { }
#pragma warning restore OE0101
```

## SystemTextJson Diagnostics

The `LayeredCraft.OptimizedEnums.SystemTextJson` generator emits diagnostics with the `OE2xxx` prefix.

### OE2001 — Not an OptimizedEnum

**Message:** `The class '{0}' must inherit from OptimizedEnum<TEnum, TValue> to use [OptimizedEnumJsonConverter]`

**Cause:** `[OptimizedEnumJsonConverter]` was applied to a class that does not inherit from `OptimizedEnum<TEnum, TValue>`.

**Fix:** Remove the attribute, or make the class inherit from `OptimizedEnum<TEnum, TValue>`.

### OE2002 — Must Be Partial

**Message:** `The class '{0}' must be declared as partial for [OptimizedEnumJsonConverter] source generation`

**Cause:** A class decorated with `[OptimizedEnumJsonConverter]` is missing the `partial` keyword. The generator cannot stamp the `[JsonConverter]` attribute onto the class.

**Fix:**
```csharp
// Before
[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
public sealed class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }

// After
[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

### OE2003 — Unknown Converter Type

**Message:** `The class '{0}' specifies an unknown OptimizedEnumJsonConverterType value '{1}'; valid values are ByName (0) and ByValue (1)`

**Cause:** An explicit integer cast was used to pass an undefined `OptimizedEnumJsonConverterType` value to `[OptimizedEnumJsonConverter]`.

**Fix:** Use only the defined enum members:
```csharp
[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]   // or ByValue
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

## EFCore Diagnostics

The `LayeredCraft.OptimizedEnums.EFCore` generator emits diagnostics with the `OE3xxx` prefix.

### OE3001 — Not an OptimizedEnum

**Message:** `The class '{0}' must inherit from OptimizedEnum<TEnum, TValue> to use [OptimizedEnumEfCore]`

**Cause:** `[OptimizedEnumEfCore]` was applied to a class that does not inherit from `OptimizedEnum<TEnum, TValue>`.

**Fix:** Remove the attribute, or make the class inherit from `OptimizedEnum<TEnum, TValue>` (directly or through an abstract intermediate base class).

### OE3002 — Must Be Partial

**Message:** `The class '{0}' must be declared as partial for [OptimizedEnumEfCore] source generation`

**Cause:** A class decorated with `[OptimizedEnumEfCore]` is missing the `partial` keyword.

**Fix:**
```csharp
// Before
[OptimizedEnumEfCore]
public sealed class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }

// After
[OptimizedEnumEfCore]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

### OE3003 — Unknown Storage Type

**Message:** `The class '{0}' specifies an unknown OptimizedEnumEfCoreStorage value '{1}'; valid values are ByValue (0) and ByName (1)`

**Cause:** An explicit integer cast was used to pass an undefined `OptimizedEnumEfCoreStorage` value to `[OptimizedEnumEfCore]`.

**Fix:** Use only the defined enum members:
```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]   // or ByName
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

### OE3004 — Unsupported Target

**Message:** `[OptimizedEnumEfCore] cannot be applied to abstract class '{0}'; apply it to a concrete sealed partial derived class`

**Cause:** `[OptimizedEnumEfCore]` was applied to an abstract class. Abstract classes cannot serve as the target for converter generation because they cannot be instantiated.

**Fix:** Apply the attribute only to concrete (`sealed`) derived classes:
```csharp
// Wrong — abstract base class
[OptimizedEnumEfCore]
public abstract partial class OrderStatusBase : OptimizedEnum<OrderStatusBase, int> { }

// Correct — concrete derived class
[OptimizedEnumEfCore]
public sealed partial class OrderStatus : OrderStatusBase<OrderStatus> { ... }
```

### OE9003 — Internal Generator Error

**Message:** `An unexpected error occurred while generating the EF Core support for '{0}': {1}`

**Cause:** An unhandled exception occurred inside the EFCore generator. This is a generator bug.

**Fix:** Report the issue with the full diagnostic message and the enum source code. As a workaround, the EFCore attribute can be temporarily removed from the affected type.

## Generator Not Running?

If you add the package but see no generated members, check:

1. **Is the class `partial`?** — OE0001 will be emitted and generation stops.
2. **Does it inherit from `OptimizedEnum<TEnum, TValue>`?** — The generator only fires when the base type is found.
3. **Are there any members?** — OE0004 fires if no qualifying fields are found.
4. **Check the build output** — run `dotnet build` and look for any `OE*` diagnostics.
5. **Inspect `obj/` generated files** — if a `.g.cs` file exists but the methods aren't available, check for a build cache issue.
