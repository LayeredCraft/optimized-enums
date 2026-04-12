# Entity Framework Core

The `LayeredCraft.OptimizedEnums.EFCore` package adds source-generated, zero-reflection Entity Framework Core value converter support for `OptimizedEnum<TEnum, TValue>` types. The generator emits concrete converter classes and registration helpers at compile time — no runtime reflection, no `Activator.CreateInstance`, and no dynamic proxy factories.

## Installation

Install the EFCore package. The core `LayeredCraft.OptimizedEnums` package is pulled in automatically:

=== ".NET CLI"

    ```bash
    dotnet add package LayeredCraft.OptimizedEnums.EFCore
    ```

=== "PackageReference"

    ```xml
    <PackageReference Include="LayeredCraft.OptimizedEnums.EFCore" Version="x.x.x" />
    ```

**EF Core version support:** EF Core 8, 9, and 10.

## Quick Start

Decorate your `OptimizedEnum` class with `[OptimizedEnumEfCore]` and add `partial` if it isn't already:

```csharp
using LayeredCraft.OptimizedEnums;
using LayeredCraft.OptimizedEnums.EFCore;

[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

Then register the conversion in your `DbContext`. The simplest option is the global convention hook:

```csharp
public class AppDbContext : DbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.ConfigureOptimizedEnums();
    }
}
```

That's all you need for basic usage. All properties of type `OrderStatus` (including nullable `OrderStatus?`) are automatically converted to and from their database representation.

## The Attribute

`[OptimizedEnumEfCore]` controls whether the enum's default storage is `ByValue` or `ByName`:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `storage` | `OptimizedEnumEfCoreStorage` | `ByValue` | How to store the enum in the database |

```csharp
// Store by underlying value (default)
[OptimizedEnumEfCore]
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]

// Store by name string
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
```

The attribute may be applied to any `sealed partial` class that inherits from `OptimizedEnum<TEnum, TValue>`, directly or through abstract intermediate base classes.

## Storage Strategies

### ByValue

Stores the enum's underlying `TValue` in the database column. The column type mirrors `TValue` (e.g., `int` → integer column, `string` → text column).

```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

- Write path: `enum.Value` — stores the integer/string value directly
- Read path: looks up the instance via the generated `TryFromValue` table
- Invalid stored values throw `InvalidOperationException` during materialization

### ByName

Stores the enum's `Name` string in the database column, regardless of `TValue`. The column is always a text/varchar column.

```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
public sealed partial class Currency : OptimizedEnum<Currency, string>
{
    public static readonly Currency Usd = new("USD", nameof(Usd));
    public static readonly Currency Eur = new("EUR", nameof(Eur));

    private Currency(string value, string name) : base(value, name) { }
}
```

- Write path: `enum.Name` — stores the string name (e.g., `"Usd"`, `"Eur"`)
- Read path: looks up the instance via the generated `TryFromName` table
- Invalid stored names throw `InvalidOperationException` during materialization

### Choosing a Strategy

| Consideration | ByValue | ByName |
|---|---|---|
| Column type | Matches `TValue` (int, string, etc.) | Always `string` |
| Human-readable in DB | Only if `TValue` is string | Yes |
| Refactor safety | Adding new members is safe; renaming a member doesn't change stored data | Adding new members is safe; **renaming a member changes stored data** |
| Query filtering | Efficient for numeric comparisons | String comparisons |

Use `ByValue` when your database is integer-keyed and you don't need to read the DB directly. Use `ByName` when human-readability matters or when `TValue` is already a meaningful string code.

## Registration Approaches

Three ways to register conversion, in order of increasing specificity:

### 1. Global Convention (recommended)

Applies the enum attribute's default storage mode to all properties of each opted-in enum type across the entire model. One call covers all entities.

Override `ConfigureConventions` in your `DbContext`:

```csharp
using LayeredCraft.OptimizedEnums.EFCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.ConfigureOptimizedEnums();  // registers all [OptimizedEnumEfCore] types
    }
}
```

EF Core's null lifting applies automatically: one registration covers both `OrderStatus` and `OrderStatus?` properties.

### 2. Enum-Specific Property Helper

Per-property extension methods are generated for each opted-in enum. Use these when you want a single property to use a different mode than the enum's attribute default:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(x => x.Status)
        .HasOrderStatusConversionByName();   // overrides the ByValue default
}
```

Both methods are always generated regardless of the enum's attribute default:

```csharp
// Generated for every opted-in enum (example: OrderStatus)
builder.HasOrderStatusConversionByValue();
builder.HasOrderStatusConversionByName();
```

The extension class is `internal` and lives in the enum's own namespace, so these methods are naturally scoped to the consuming assembly.

### 3. Direct Converter

You can also pass a converter instance directly using EF Core's standard API:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(x => x.Status)
        .HasConversion(new OrderStatusValueConverter());    // ByValue
        // or:
        .HasConversion(new OrderStatusNameConverter());     // ByName
}
```

### Precedence

When multiple registrations exist for the same property, EF Core's own precedence applies:

1. Explicit property override (`HasConversion(...)` or `HasXxxConversionByValue/ByName()` in `OnModelCreating`)
2. Convention registered via `ConfigureConventions`

Property-level configuration always wins over convention-level configuration.

## What Gets Generated

For each opted-in enum, the generator emits a single `.g.cs` file containing:

**Converter classes** (always both, regardless of attribute default):

```csharp
// ByValue converter — converts between OrderStatus and int
internal sealed class OrderStatusValueConverter
    : ValueConverter<global::MyApp.Domain.OrderStatus, int>
{
    public OrderStatusValueConverter()
        : base(static v => v.Value, static v => FromValue(v)) { }

    private static global::MyApp.Domain.OrderStatus FromValue(int v) =>
        global::MyApp.Domain.OrderStatus.TryFromValue(v, out var result)
            ? result!
            : throw new InvalidOperationException($"'{v}' is not a valid value for OrderStatus.");
}

// ByName converter — converts between OrderStatus and string
internal sealed class OrderStatusNameConverter
    : ValueConverter<global::MyApp.Domain.OrderStatus, string>
{
    public OrderStatusNameConverter()
        : base(static v => v.Name, static v => FromName(v)) { }

    private static global::MyApp.Domain.OrderStatus FromName(string v) =>
        global::MyApp.Domain.OrderStatus.TryFromName(v, out var result)
            ? result!
            : throw new InvalidOperationException($"'{v}' is not a valid name for OrderStatus.");
}
```

**Extension class** with property-builder helpers:

```csharp
internal static class MyApp_Domain_OrderStatusEfCoreExtensions
{
    public static PropertyBuilder<global::MyApp.Domain.OrderStatus> HasOrderStatusConversionByValue(
        this PropertyBuilder<global::MyApp.Domain.OrderStatus> builder)
    {
        builder.HasConversion<global::MyApp.Domain.OrderStatusValueConverter>();
        return builder;
    }

    public static PropertyBuilder<global::MyApp.Domain.OrderStatus> HasOrderStatusConversionByName(
        this PropertyBuilder<global::MyApp.Domain.OrderStatus> builder)
    {
        builder.HasConversion<global::MyApp.Domain.OrderStatusNameConverter>();
        return builder;
    }
}
```

**Shared conventions file** (emitted once per compilation):

```csharp
namespace LayeredCraft.OptimizedEnums.EFCore
{
    public static class OptimizedEnumEfCoreConventionExtensions
    {
        public static ModelConfigurationBuilder ConfigureOptimizedEnums(
            this ModelConfigurationBuilder builder)
        {
            builder.Properties<global::MyApp.Domain.OrderStatus>()
                .HaveConversion<global::MyApp.Domain.OrderStatusValueConverter>();
            // ... one entry per opted-in enum
            return builder;
        }
    }
}
```

The conventions file is always emitted, even when no enums are opted in, so `builder.ConfigureOptimizedEnums()` compiles before any annotation is added.

## Nullable Properties

Nullable properties (`OrderStatus?`) work without any extra configuration. EF Core automatically lifts null through the converter:

- `null` in the database → `null` in the CLR property
- Non-null database value → converted to the appropriate `OrderStatus` instance
- Non-null invalid value → `InvalidOperationException` during materialization

No separate nullable converter class is generated — EF Core's null lifting handles this automatically from the non-nullable converter.

```csharp
public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }       // non-nullable
    public OrderStatus? OptionalStatus { get; set; }  // nullable — works automatically
}
```

## Primary Keys, Foreign Keys, and Indexes

The generated converters work for all standard EF Core property roles:

```csharp
// Primary key
public class StatusRecord
{
    public OrderStatus Id { get; set; }  // OrderStatus as PK
    public string Description { get; set; }
}

// Foreign key
public class Order
{
    public OrderStatus StatusCode { get; set; }
    public StatusRecord Status { get; set; }  // navigation
}

// Alternate key
modelBuilder.Entity<Order>()
    .HasAlternateKey(x => x.Status);

// Index
modelBuilder.Entity<Order>()
    .HasIndex(x => x.Status);
```

Register the converters as normal (via `ConfigureOptimizedEnums()` or explicit property configuration) and these scenarios work without additional setup.

## String-Valued Enums

`ByValue` and `ByName` both work with `string`-typed `TValue`. The provider type is `string` for `ByValue` (stores the raw value) and also `string` for `ByName` (stores the name). If your value and name are different strings, choose the one you want in the database:

```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class Currency : OptimizedEnum<Currency, string>
{
    // Value = ISO code, Name = property name
    public static readonly Currency Usd = new("USD", nameof(Usd));
    public static readonly Currency Eur = new("EUR", nameof(Eur));

    private Currency(string value, string name) : base(value, name) { }
}
```

With `ByValue`, the database stores `"USD"` / `"EUR"`. With `ByName`, it stores `"Usd"` / `"Eur"`.

## Abstract Intermediate Base Classes

The generator correctly resolves the `OptimizedEnum<TEnum, TValue>` base through one or more abstract intermediate classes. Annotate only the concrete sealed class:

```csharp
// Abstract base — not annotated, does not need to be partial
public abstract class OrderStatusBase<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
{
    protected OrderStatusBase(int value, string name) : base(value, name) { }
}

// Concrete enum — annotated and partial
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class OrderStatus : OrderStatusBase<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

Applying `[OptimizedEnumEfCore]` to an abstract class is an error (OE3004).

## Nested Types

Enums nested inside other types are fully supported:

```csharp
public partial class Outer
{
    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
    public sealed partial class Status : OptimizedEnum<Status, int>
    {
        public static readonly Status Active   = new(1, nameof(Active));
        public static readonly Status Inactive = new(2, nameof(Inactive));

        private Status(int value, string name) : base(value, name) { }
    }
}
```

The generated converter and extension class names include the containing type name with a `_` separator to avoid collisions (e.g., `Outer_StatusValueConverter`, `MyApp_Domain_Outer_StatusEfCoreExtensions`).

**Limitation:** Enums nested inside **generic** containing types are not supported and produce OE3004. Converters and extension methods are emitted at namespace scope; generic type parameters from the containing type would not be in scope there. Move the enum out of the generic container, or register the conversion manually.

## Extension Class Naming

The generated extension class is named by joining all namespace segments and containing-type names with underscores, suffixed with `EfCoreExtensions`:

| Enum location | Extension class name |
|---|---|
| `MyApp.Domain.OrderStatus` | `MyApp_Domain_OrderStatusEfCoreExtensions` |
| `MyApp.Domain.Outer.Status` | `MyApp_Domain_Outer_StatusEfCoreExtensions` |
| `Priority` (global namespace) | `PriorityEfCoreExtensions` |

This scheme prevents name collisions between enums with the same class name in different namespaces.

## Invalid Value Behavior

The generated converters throw `InvalidOperationException` for unrecognized provider values:

```
System.InvalidOperationException: '99' is not a valid value for OrderStatus.
System.InvalidOperationException: 'Unknown' is not a valid name for OrderStatus.
```

This happens during EF Core's materialization phase (when reading from the database). The exception propagates through EF's normal error handling. Do not store values in the database that are not defined members of the enum.

## AOT and Reflection-Free Design

The generated code contains zero package-authored runtime reflection:

- Converter classes are concrete and non-generic — no `MakeGenericType`
- Lookup logic delegates to the source-generated `TryFromValue` / `TryFromName` static dictionary methods on the enum class
- No `Activator.CreateInstance`, no `Delegate.CreateDelegate`, no runtime type-walking

The `HasConversion<TConverter>()` call in the extension methods and the `HaveConversion<TConverter>()` call in `ConfigureOptimizedEnums()` use generic type parameters that are known at compile time. EF Core may use reflection internally to instantiate the converter class at startup, but the converter logic itself is entirely reflection-free.

## Diagnostics

The EFCore generator emits diagnostics with the `OE3xxx` prefix. See [Diagnostics — EFCore](../advanced/diagnostics.md#efcore-diagnostics) for the full list.

## v1 Limitations

The following are intentionally deferred to a future version:

- **Generic property-builder helpers** — `HasOptimizedEnumConversionByValue<TEnum, TValue>()` cannot be implemented without reflection or static abstract interface members, because `TryFromValue` / `TryFromName` are generated on each concrete class and not accessible from a base-type constraint. Use the enum-specific helpers or the global convention instead.
- **Custom `[OptimizedEnumIndex]` persistence** — per-property custom indexes defined via `[OptimizedEnumIndex]` are not mapped to database columns.
- **Automatic schema hints** — no generated string-length, unicode, or column-type annotations. Apply these manually via fluent API if needed.
- **Collection mapping** — e.g., `ICollection<OrderStatus>` properties are not specially handled.
