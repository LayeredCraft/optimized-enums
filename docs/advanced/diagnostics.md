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

## Generator Not Running?

If you add the package but see no generated members, check:

1. **Is the class `partial`?** — OE0001 will be emitted and generation stops.
2. **Does it inherit from `OptimizedEnum<TEnum, TValue>`?** — The generator only fires when the base type is found.
3. **Are there any members?** — OE0004 fires if no qualifying fields are found.
4. **Check the build output** — run `dotnet build` and look for any `OE*` diagnostics.
5. **Inspect `obj/` generated files** — if a `.g.cs` file exists but the methods aren't available, check for a build cache issue.
