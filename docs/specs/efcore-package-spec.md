# LayeredCraft.OptimizedEnums.EFCore Technical Specification

## Status

- Confirmed — design interview complete (2026-04-10)
- Intended audience: implementation agent / reviewer
- Goal: detailed enough to implement without additional product discovery

## Summary

Add a new package, `LayeredCraft.OptimizedEnums.EFCore`, that provides Entity Framework Core support for `OptimizedEnum<TEnum, TValue>` using source generation instead of runtime reflection.

The package must preserve the core library's design goals:

- zero reflection in package-authored conversion logic
- AOT-safe generated code
- compile-time validation where possible
- explicit, concrete generated code instead of runtime factories

This package is conceptually similar to `Ardalis.SmartEnum.EFCore`, but it must not copy SmartEnum's runtime reflection approach. It should instead follow the existing repository pattern already used by `LayeredCraft.OptimizedEnums.SystemTextJson`:

- a single NuGet package that contains a source generator
- post-initialization attribute injection into the consumer compilation
- syntax-driven compile-time discovery of opted-in enum types
- concrete generated helpers per enum type

## Product Requirements

These requirements were explicitly confirmed.

### Package and platform

- Package name: `LayeredCraft.OptimizedEnums.EFCore`
- Package shape: single package
- EF Core versions supported in v1: `8`, `9`, and `10`
- The package should fit this repo's existing packaging style for generator-based extensions.

### Opt-in model

- Support both enum-level and fluent opt-in.
- Enum-level opt-in is done with a generated attribute.
- Fluent opt-in is done with generated extension methods.
- Global convention opt-in is supported.

### Storage behavior

- Package-level default storage mode: `ByValue`
- Enum-level attribute sets the enum's default storage mode.
- Per-property override is supported.
- Precedence is:
  1. property override
  2. enum attribute default
  3. package default (`ByValue`)
- `ByName` always stores the enum member's `Name` string, regardless of `TValue`.

### Supported scenarios in v1

- scalar properties
- nullable scalar properties
- primary keys
- foreign keys
- alternate keys
- indexes
- enums that inherit through abstract intermediate optimized-enum base classes

### Unsupported / deferred in v1

- persistence using `[OptimizedEnumIndex]`-defined custom indexes
- automatic schema hints such as string length, unicode, or explicit column types
- collection mapping support
- owned type special handling beyond whatever naturally works through generated property APIs
- any runtime scanning mechanism that depends on reflection to discover enum types dynamically

### Failure behavior

- invalid non-null provider values must throw during materialization
- nullable property + database null maps to CLR null
- nullable property + invalid non-null provider value throws
- non-nullable property + provider null should fail through EF/runtime behavior
- invalid compile-time configuration should produce diagnostics wherever feasible
- invalid runtime-only usage should throw clear exceptions

## Non-Goals

- Do not implement a runtime reflection-based converter factory.
- Do not implement automatic relational schema conventions in v1.
- Do not add custom index persistence strategies in v1.
- Do not add backward-compatibility shims for hypothetical future APIs.
- Do not require users to hand-author converter classes.

## Existing Repo Constraints

This spec must fit the current repository conventions.

### Existing package model

- `src/LayeredCraft.OptimizedEnums.Generator` is the main package shipped as `LayeredCraft.OptimizedEnums`.
- `src/LayeredCraft.OptimizedEnums.SystemTextJson.Generator` is a second shipped package that contains a generator and injects an attribute.
- The runtime project `src/LayeredCraft.OptimizedEnums` targets `netstandard2.0` and is intentionally not directly packed as the primary user-facing package.
- The STJ package is the closest architectural precedent and should be treated as the primary model for structure and packaging.

### Testing model

- Tests are multi-targeted: `net8.0;net9.0;net10.0`.
- Tests use xUnit v3 and Microsoft.Testing.Platform.
- Generator tests use Verify snapshots.
- Focused tests use `dotnet test --project ... -- --filter-method "*MethodName"`.

### Documentation model

- User docs live under `docs/usage/`, `docs/advanced/`, and related sections.
- Package diagnostics are documented in `docs/advanced/diagnostics.md`.
- README should mention extension packages and installation.

## High-Level Design

The package should generate compile-time EF Core support for explicitly opted-in optimized enums.

The consumer experience should look like this:

```csharp
using LayeredCraft.OptimizedEnums;
using LayeredCraft.OptimizedEnums.EFCore;

[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

Then EF usage can be any of the following:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder builder)
{
    builder.ConfigureOptimizedEnums();
}
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(x => x.Status)
        .HasOptimizedEnumConversionByName();
}
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(x => x.Status)
        .HasOrderStatusConversionByValue();
}
```

The package-generated code should be concrete and direct. It should not use `MakeGenericType`, `Activator.CreateInstance`, or runtime base-type walking for the actual conversion path.

## Public API Specification

## Package namespace

Generated public-facing EF types should live under:

```csharp
namespace LayeredCraft.OptimizedEnums.EFCore;
```

## Injected attribute source

The generator must inject an attribute definition into the consumer compilation via `RegisterPostInitializationOutput`, mirroring the SystemTextJson package.

### Required generated types

```csharp
namespace LayeredCraft.OptimizedEnums.EFCore
{
    public enum OptimizedEnumEfCoreStorage
    {
        ByValue = 0,
        ByName = 1,
    }

    [global::System.AttributeUsage(
        global::System.AttributeTargets.Class,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class OptimizedEnumEfCoreAttribute : global::System.Attribute
    {
        public OptimizedEnumEfCoreAttribute(
            OptimizedEnumEfCoreStorage storage = OptimizedEnumEfCoreStorage.ByValue)
        {
            Storage = storage;
        }

        public OptimizedEnumEfCoreStorage Storage { get; }
    }
}
```

### Attribute semantics

- The attribute is applied to the enum type.
- The attribute indicates that EF Core support should be generated for that enum.
- The attribute's `Storage` value defines the enum-level default.
- Omitting the constructor argument is equivalent to `ByValue`.

### Attribute usage examples

```csharp
[OptimizedEnumEfCore]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
public sealed partial class Currency : OptimizedEnum<Currency, string> { ... }
```

## Fluent API surface

The v1 public API surface consists of two things only:

1. Enum-specific generated property helpers (per opted-in enum)
2. Generated global convention registration (`ConfigureOptimizedEnums()`)

### Why generic helpers are deferred to v2

Generic helpers (`HasOptimizedEnumConversionByValue<TEnum, TValue>()`) cannot be implemented without reflection or static abstract interface members. The generated `TryFromValue` and `TryFromName` methods are emitted on each concrete partial class by the core generator — they are not members of the `OptimizedEnum<TEnum, TValue>` base class and are therefore not accessible from a generic method constrained only to the base type.

The guiding rule: anything that requires generated lookup methods to work cannot be compiled into the DLL and cannot be expressed as a simple generic helper. Generic helpers are deferred to v2, where they can be revisited alongside a possible base-class interface addition.

### Enum-specific property APIs

For every opted-in enum, generate explicit methods that remove ambiguity and improve discoverability.

Example for `OrderStatus`:

```csharp
public static class OrderStatusEfCoreExtensions
{
    public static PropertyBuilder<global::MyApp.Domain.OrderStatus> HasOrderStatusConversionByValue(
        this PropertyBuilder<global::MyApp.Domain.OrderStatus> builder);

    public static PropertyBuilder<global::MyApp.Domain.OrderStatus?> HasOrderStatusConversionByValue(
        this PropertyBuilder<global::MyApp.Domain.OrderStatus?> builder);

    public static PropertyBuilder<global::MyApp.Domain.OrderStatus> HasOrderStatusConversionByName(
        this PropertyBuilder<global::MyApp.Domain.OrderStatus> builder);

    public static PropertyBuilder<global::MyApp.Domain.OrderStatus?> HasOrderStatusConversionByName(
        this PropertyBuilder<global::MyApp.Domain.OrderStatus?> builder);
}
```

These methods apply the appropriate converter and comparer together via `HasConversion` + `HasComparer`.

### Extension class naming

The generated extension class is named by joining the fully-qualified enum name segments with underscores, suffixed with `EfCoreExtensions`. This avoids collisions when two enums in different namespaces share the same class name.

Examples:

- `MyApp.Domain.OrderStatus` → `MyApp_Domain_OrderStatusEfCoreExtensions`
- `MyApp.Domain1.Status` → `MyApp_Domain1_StatusEfCoreExtensions`
- `MyApp.Domain2.Status` → `MyApp_Domain2_StatusEfCoreExtensions`
- Global namespace `Priority` → `PriorityEfCoreExtensions`

### Convention / model configuration APIs

Global convention support is required.

Minimum target shape:

```csharp
public static class OptimizedEnumEfCoreConventionExtensions
{
    public static ModelConfigurationBuilder ConfigureOptimizedEnums(
        this ModelConfigurationBuilder builder);
}
```

Behavior:

- Applies enum-level defaults for all enums annotated with `[OptimizedEnumEfCore]`.
- The implementation must be generated from the known set of opted-in enums in the consuming compilation.
- It must not depend on runtime reflection-based model scanning to discover enum types.

If EF version differences require additional overloads or alternative builder surfaces, those may be added, but the above experience is the minimum target.

## Generated Runtime Types

For each opted-in enum, the generator must emit concrete EF Core helper types.

Example names for enum `OrderStatus`:

- `OrderStatusValueConverter`
- `OrderStatusNameConverter`
- `OrderStatusValueComparer`
- enum-specific extension container class

Both `ByValue` and `ByName` converters are always generated for every opted-in enum, regardless of the enum attribute's default storage mode. This is required because per-property overrides allow callers to switch between modes at any point.

### Value converter requirements

For `ByValue`, generate a converter roughly equivalent to:

```csharp
internal sealed class OrderStatusValueConverter
    : ValueConverter<global::MyApp.Domain.OrderStatus, int>
{
    public OrderStatusValueConverter()
        : base(
            value => value.Value,
            value => global::MyApp.Domain.OrderStatus.TryFromValue(value, out var result)
                ? result!
                : throw new InvalidOperationException(
                    $"'{value}' is not a valid value for OrderStatus."))
    {
    }
}
```

For `ByName`, generate a converter roughly equivalent to:

```csharp
internal sealed class OrderStatusNameConverter
    : ValueConverter<global::MyApp.Domain.OrderStatus, string>
{
    public OrderStatusNameConverter()
        : base(
            value => value.Name,
            value => global::MyApp.Domain.OrderStatus.TryFromName(value, out var result)
                ? result!
                : throw new InvalidOperationException(
                    $"'{value}' is not a valid name for OrderStatus."))
    {
    }
}
```

### Converter behavior rules

#### ByValue

- Provider type is exactly `TValue`.
- Write path returns `enum.Value`.
- Read path uses generated optimized-enum lookup by value.
- Invalid provider values throw.

#### ByName

- Provider type is `string`.
- Write path returns `enum.Name`.
- Read path uses generated optimized-enum lookup by name.
- Name matching must behave the same way as the generated `TryFromName` lookup.
- Invalid provider values throw.

### Null handling in converters

Generated converters use non-nullable types: `ValueConverter<TEnum, TProvider>`. EF Core automatically lifts null through the converter for nullable properties (`OrderStatus?`), so no nullable-aware converter variant is needed.

This means:

- One converter class handles both `OrderStatus` and `OrderStatus?` properties.
- The convention hook registers once for the non-nullable type; EF applies the converter to nullable properties of the same type automatically.
- The generated converter code does not need to handle null on either the write or read path.

### Value comparer requirements

The implementation must generate or apply a comparer if EF requires one for stable tracking, keys, or change detection.

Required behavior:

- Two enum instances compare equal if the underlying optimized-enum equality says they are equal.
- Hashing must remain consistent with optimized-enum equality.
- Snapshot behavior must be correct for immutable optimized-enum instances.

Preferred comparer logic:

- equality: `left == right` or `Equals(left, right)`
- hash: `value == null ? 0 : value.GetHashCode()`
- snapshot: return the same instance because optimized enums are immutable singletons

If implementation testing proves that a custom comparer is unnecessary for some scenarios, it may still be generated uniformly for consistency.

## Discovery and Generation Rules

## Opted-in target discovery

The syntax provider must discover classes annotated with:

```text
LayeredCraft.OptimizedEnums.EFCore.OptimizedEnumEfCoreAttribute
```

### Valid target requirements

The target must:

- be a class
- inherit from `OptimizedEnum<TEnum, TValue>` either directly or through intermediate abstract bases
- be declared `partial`
- have a resolvable enum-level storage mode value of `ByValue` or `ByName`

### Captured model data

For each valid target, the generation model must capture at least:

- namespace or null for global namespace
- class name
- fully-qualified class name
- fully-qualified provider value type
- whether `TValue` is a reference type
- containing type declarations for nested types
- selected enum-level default storage mode
- diagnostic list
- source location

This should closely mirror the data shape used by the existing STJ generator.

## Inheritance handling

The syntax provider must correctly resolve the `OptimizedEnum<TEnum, TValue>` base even when the concrete enum inherits through one or more abstract intermediate base classes.

Supported example:

```csharp
public abstract class OrderStatusBase<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
{
    protected OrderStatusBase(int value, string name) : base(value, name) { }
}

[OptimizedEnumEfCore]
public sealed partial class OrderStatus : OrderStatusBase<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

## Namespaces and nesting

Generated code must work correctly for:

- namespace-scoped enums
- global namespace enums
- nested optimized-enum types

The STJ generator's pattern for preamble/suffix generation is the preferred precedent.

## Precedence Rules

The following precedence is mandatory:

1. explicit property override
2. enum attribute default
3. package default (`ByValue`)

### Examples

#### No property override

```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

With `ConfigureOptimizedEnums()`, `OrderStatus` properties store `Name`.

#### Property override supersedes enum default

```csharp
[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }

builder.Entity<Order>()
    .Property(x => x.Status)
    .HasOptimizedEnumConversionByName();
```

This property stores `Name`, not `Value`.

## Diagnostics Specification

Use a new EFCore-specific diagnostic range with prefix `OE3xxx`.

These identifiers are reserved by this spec.

### OE3001 - Not an OptimizedEnum

- Severity: Error
- Message:
  `The class '{0}' must inherit from OptimizedEnum<TEnum, TValue> to use [OptimizedEnumEfCore]`
- Trigger:
  attribute applied to a class that does not inherit from the optimized-enum base

### OE3002 - Must Be Partial

- Severity: Error
- Message:
  `The class '{0}' must be declared as partial for [OptimizedEnumEfCore] source generation`
- Trigger:
  attribute applied to a non-partial class

### OE3003 - Unknown Storage Type

- Severity: Error
- Message:
  `The class '{0}' specifies an unknown OptimizedEnumEfCoreStorage value '{1}'; valid values are ByValue (0) and ByName (1)`
- Trigger:
  invalid cast or undefined enum value passed to the attribute constructor

### OE3004 - Unsupported EF Core Target Usage

- Severity: Error
- Message:
  implementation-defined, but should clearly explain the unsupported target or configuration
- Confirmed triggers:
  - `[OptimizedEnumEfCore]` applied to an abstract class — message should say the attribute cannot be applied to abstract classes and must be applied to concrete sealed partial derived classes
- May also cover other unsupported target configurations discovered during generation

### OE9003 - Internal Generator Error

- Severity: Error
- Message:
  `An unexpected error occurred while generating the EF Core support for '{0}': {1}`
- Trigger:
  template/render/generation exception
- Note: ID is OE9003 (not OE3999) to match the STJ package's OE9002 pattern for internal generator errors across packages

### Diagnostic policy

- Errors block code emission for that target enum.
- Diagnostics should be attached to the annotated enum declaration when possible.
- Diagnostics should mirror the clarity and directness of the STJ package diagnostics.

## Runtime Exception Policy

Some failures cannot be caught during source generation. Those should throw clear runtime exceptions.

### Required runtime failures

- invalid provider value for `ByValue`
- invalid provider value for `ByName`
- impossible misuse of a generated extension method that cannot be expressed as a compile-time diagnostic

### Exception guidance

- Prefer `InvalidOperationException` for invalid persisted values.
- Error text should include the invalid value and the enum name.
- Do not swallow provider values or silently coerce unknown values to null.

## Project Structure Specification

Add a new project:

```text
src/LayeredCraft.OptimizedEnums.EFCore.Generator/
```

Expected structure:

```text
src/LayeredCraft.OptimizedEnums.EFCore.Generator/
  AnalyzerReleases.Shipped.md
  AnalyzerReleases.Unshipped.md
  AttributeSource.cs
  LayeredCraft.OptimizedEnums.EFCore.Generator.csproj
  OptimizedEnumEfCoreGenerator.cs
  TrackingNames.cs
  Diagnostics/
    DiagnosticDescriptors.cs
    DiagnosticInfo.cs
  Emitters/
    EfCoreEmitter.cs
    TemplateHelper.cs
  Models/
    EfCoreInfo.cs
    EquatableArray.cs
    LocationInfo.cs
  Providers/
    EfCoreSyntaxProvider.cs
  Templates/
    OptimizedEnumEfCore.scriban
```

### Reuse guidance

The implementation may copy or adapt the STJ package's supporting infrastructure where appropriate:

- `EquatableArray`
- `LocationInfo`
- `DiagnosticInfo`
- `TemplateHelper`
- tracking-name conventions

Avoid clever abstraction between packages unless it is clearly worth the added complexity. A small amount of duplication is acceptable if it keeps each package simple and self-contained.

## Project File Specification

The project should follow the packaging pattern of `LayeredCraft.OptimizedEnums.SystemTextJson.Generator`.

### Required characteristics

- SDK-style project
- `TargetFramework` = `netstandard2.0`
- `IncludeBuildOutput` = `false`
- `IsPackable` = `true`
- `AssemblyName` should be generator-specific, for example `LayeredCraft.OptimizedEnums.EFCore.Generator`
- `PackageId` must be `LayeredCraft.OptimizedEnums.EFCore`
- embed Scriban templates as resources
- pack the generator assembly into `analyzers/dotnet/cs`
- reference the main optimized-enum generator package/project so consumers also get the core package path they need

### Dependencies

The package will need EF Core API references sufficient for generated code and tests.

Implementation constraints:

- choose dependency declarations that allow EF Core 8, 9, and 10 consumers
- generated code should use API shapes stable across those versions
- avoid `Microsoft.EntityFrameworkCore.Relational` unless required
- do not add dependencies not needed by emitted code

This version of the spec intentionally does not lock exact version-range syntax because that must be validated against NuGet resolution and the repo's central package management.

## Generation Output Specification

For each valid annotated enum, the generator should emit a single `.g.cs` file containing all EF Core support for that enum, plus the post-init attribute source once per compilation.

### Single-target output contents

For one enum, the generated file should include:

- any necessary using-free fully-qualified references
- `GeneratedCode` attributes
- concrete comparer type
- concrete `ByValue` converter type
- concrete `ByName` converter type
- enum-specific property-builder extension methods
- any enum-specific convention registration helpers if needed

### Shared/global output contents

The generator may also emit a shared helpers file if that materially simplifies implementation, but prefer minimal shared global output unless needed.

If shared output is emitted, it must remain deterministic and avoid name collisions.

## Convention Registration Design

The global convention hook is one of the key product requirements.

### Required consumer experience

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder builder)
{
    builder.ConfigureOptimizedEnums();
}
```

### Required behavior

- The method applies default conversion for all annotated enums in the consumer compilation.
- For each annotated enum, the method uses the enum attribute's `Storage` value.
- If the attribute is omitted for an enum, that enum is not included in this global hook.
- Explicit per-property configuration later in model configuration must still be able to override the convention.

### Implementation guidance

The generated global hook registers both converter and comparer for each opted-in enum:

```csharp
builder.Properties<global::MyApp.Domain.OrderStatus>()
    .HaveConversion<global::MyApp.Domain.OrderStatusValueConverter>()
    .HaveValueComparer<global::MyApp.Domain.OrderStatusValueComparer>();
```

One registration per enum covers both nullable and non-nullable properties — EF Core's null lifting applies the converter automatically when the property type is `OrderStatus?`.

The shared conventions file is always emitted, even when no enums are opted in. This ensures `builder.ConfigureOptimizedEnums()` compiles even before any enum is annotated:

```csharp
// Generated with zero opted-in enums:
public static ModelConfigurationBuilder ConfigureOptimizedEnums(
    this ModelConfigurationBuilder builder)
{
    // no-op until enums are annotated
    return builder;
}
```

If exact API signatures differ across EF versions, the implementation may need to choose the most stable shared pattern. The public user experience must remain `builder.ConfigureOptimizedEnums()`.

## EF Modeling Scope

The generated support must be validated for the following model use cases.

### Scalar property

```csharp
public OrderStatus Status { get; set; }
```

### Nullable scalar property

```csharp
public OrderStatus? Status { get; set; }
```

### Primary key

```csharp
public OrderStatus Id { get; set; }
```

### Foreign key

```csharp
public OrderStatus StatusId { get; set; }
public StatusEntity Status { get; set; }
```

### Alternate key

```csharp
builder.Entity<Order>()
    .HasAlternateKey(x => x.Status);
```

### Index

```csharp
builder.Entity<Order>()
    .HasIndex(x => x.Status);
```

### Important limitation

Support for primary keys / foreign keys / alternate keys / indexes means EF must be able to model and persist these properties correctly when the generated conversions are applied. It does not imply any support for persisting alternate custom optimized-enum indexes defined by `[OptimizedEnumIndex]`.

## Nullability and Provider-Type Rules

### ByValue provider type

- provider type is `TValue`
- for nullable enum properties, the effective provider flow may need to handle nullable provider values depending on EF's converter API shape

### ByName provider type

- provider type is `string`
- provider null for nullable property maps to CLR null
- invalid non-null strings throw

### Write-path assumptions

- enum properties are expected to be valid optimized-enum instances
- generated code does not need to support arbitrary subclass instances outside the optimized-enum contract

## Testing Specification

Two categories of tests are required.

## 1. Generator snapshot tests

Add a single test project containing both generator snapshot tests and EF runtime/integration tests:

```text
tests/LayeredCraft.OptimizedEnums.EFCore.Tests/
  GeneratorTests/     ← snapshot test classes
  IntegrationTests/   ← EF runtime test classes
  Snapshots/          ← Verify *.verified.cs files
```

### Generator test project requirements

- multi-target `net8.0;net9.0;net10.0`
- use xUnit v3 + MTP
- use Verify snapshots
- reference:
  - `src/LayeredCraft.OptimizedEnums`
  - `src/LayeredCraft.OptimizedEnums.Generator`
  - new EFCore generator project
  - EF Core package references needed to compile generated code in test compilations
- additional test dependencies:
  - `Microsoft.EntityFrameworkCore.InMemory` — for basic conversion and null behavior tests
  - `Testcontainers.PostgreSql` — for relational integration tests
  - `Npgsql.EntityFrameworkCore.PostgreSQL` — EF Core provider for PostgreSQL

### Snapshot test cases

At minimum cover:

- `ByValue_WithNamespace`
- `ByName_WithNamespace`
- `ByValue_GlobalNamespace`
- `ByName_GlobalNamespace`
- `ByValue_StringValueType`
- `ByName_StringValueType`
- `NestedType`
- `IntermediateAbstractBase`
- `Error_NotOptimizedEnum`
- `Error_NotPartial`
- `Error_UnknownStorageType`

### Snapshot assertions

- no unexpected diagnostics for valid inputs
- expected diagnostic id for invalid inputs
- generated code compiles after reparsing trees with the same parse options
- generated tree count is stable where asserted
- snapshot scrubber should remove generator version numbers from `GeneratedCode` attributes

## 2. EF runtime/integration tests

Runtime tests live in the same project under `IntegrationTests/`.

### Runtime provider guidance

Two providers are used, split by test concern:

- **InMemory** (`Microsoft.EntityFrameworkCore.InMemory`): basic conversion, null behavior, and materialization tests. Fast, no Docker dependency.
- **PostgreSQL via Testcontainers** (`Testcontainers.PostgreSql` + `Npgsql.EntityFrameworkCore.PostgreSQL`): relational scenarios requiring real schema — primary keys, foreign keys, alternate keys, and indexes. Uses `PostgreSqlBuilder` / `PostgreSqlContainer`.

The following behaviors must be verified.

### Runtime test matrix

#### Conversion basics (InMemory)

- save/load `ByValue`
- save/load `ByName`
- enum default via attribute works through global convention hook
- explicit property override wins over enum default

#### Null behavior (InMemory)

- nullable property round-trips null
- invalid non-null stored value throws on materialization
- non-nullable property with database null fails

#### Model semantics (PostgreSQL via Testcontainers)

- property configured as primary key works
- property configured as foreign key works
- property configured as alternate key works
- property configured with index works

#### Type variety (InMemory or PostgreSQL as appropriate)

- integer-valued enum
- string-valued enum
- intermediate-base enum

#### API surface (InMemory)

- enum-specific builder helpers compile and work
- global convention helper compiles and works

## Documentation Deliverables

Implementation should also add or update documentation.

### README.md

- add installation snippet for `LayeredCraft.OptimizedEnums.EFCore`
- include one short example showing attribute plus `ConfigureConventions`

### `docs/usage/ef-core.md`

Create a dedicated EF Core usage page covering:

- installation
- attribute usage
- `ByValue` vs `ByName`
- global convention registration
- enum-specific property overrides
- precedence rules
- key/index support
- invalid value behavior
- AOT / reflection-free design notes
- v1 limitations (including deferred generic helpers)

### `docs/advanced/diagnostics.md`

Add EFCore diagnostics section for `OE3xxx`.

### Optional docs updates

- docs navigation / index updates if the docs site requires explicit linking
- changelog entry if this repo tracks pending changes there

## Solution and Repo Wiring

Implementation should update the solution and supporting repo files as needed.

### Expected wiring changes

- add new project to `LayeredCraft.OptimizedEnums.slnx`
- add new test project(s) to `LayeredCraft.OptimizedEnums.slnx`
- add central package versions to `Directory.Packages.props` for EF Core packages introduced
- ensure docs references are included in the solution if this repo keeps docs files listed there

## Verification Commands

These are the expected verification commands for implementation work.

### Full build

```bash
dotnet build LayeredCraft.OptimizedEnums.slnx -v minimal
```

### Full test run

```bash
dotnet test --solution LayeredCraft.OptimizedEnums.slnx -v minimal
```

### Focused generator test project

```bash
dotnet test --project tests/LayeredCraft.OptimizedEnums.EFCore.Tests/LayeredCraft.OptimizedEnums.EFCore.Tests.csproj
```

### Focused xUnit method

```bash
dotnet test --project tests/LayeredCraft.OptimizedEnums.EFCore.Tests/LayeredCraft.OptimizedEnums.EFCore.Tests.csproj -- --filter-method "*ByValue_WithNamespace"
```

### Docs build

```bash
uv sync --locked --all-extras --dev
uv run zensical build --clean
```

## Acceptance Criteria

The feature is complete when all of the following are true.

### Packaging

- `LayeredCraft.OptimizedEnums.EFCore` packs successfully as a single package
- consumer installation of that one package is sufficient to use the feature

### Generation

- `[OptimizedEnumEfCore]` is injected into the consumer compilation
- valid annotated enums generate EF Core support without diagnostics
- invalid inputs produce the expected `OE3xxx` diagnostics

### Public usage

- `ConfigureOptimizedEnums()` works
- generic property-builder overrides work
- enum-specific property-builder overrides work
- precedence rules behave exactly as specified

### Runtime behavior

- `ByValue` and `ByName` both persist and materialize correctly
- null behavior matches the confirmed rules
- invalid persisted values throw
- scalar, nullable, PK, FK, alternate key, and index scenarios work

### Quality constraints

- generated conversion logic contains no package-authored runtime reflection
- implementation is AOT-safe in the same sense as the rest of the repo's generated support
- docs and diagnostics are added alongside code

## Implementation Notes and Guidance

These notes are not product requirements, but they are strong guidance for the implementation agent.

### Favor the STJ pattern directly

Do not invent a new generator architecture unless needed. The simplest approach is to mirror the STJ package:

- injected attribute source
- syntax provider to build a compact immutable model
- source emission through a Scriban template
- diagnostics emitted from the model

### Prefer concrete generated code over generic runtime infrastructure

It is acceptable to have a small shared helper if it genuinely simplifies the generated code, but prefer direct generated converter classes because they are easier to reason about, snapshot-test, and keep AOT-safe.

### Use fully-qualified names in generated code

Generated code should prefer `global::`-qualified references to avoid namespace collisions and to match the rest of the repo.

### Keep the initial version focused

If tradeoffs are required during implementation, keep the implementation aligned to the confirmed v1 scope and defer anything extra.

Priority order:

1. correct conversion generation
2. convention hook
3. diagnostics
4. test coverage
5. docs polish

### Be explicit about any EF-version compromises

If a single API surface cannot be shared across EF 8/9/10 exactly as written, preserve the confirmed external behavior and document the exact technical compromise in code comments or implementation notes.

## Design Decisions (confirmed 2026-04-10)

These decisions were made during a design interview and are binding for the v1 implementation.

| Decision | Resolution |
|---|---|
| Generic property builder helpers | Deferred to v2. `TryFromValue`/`TryFromName` are generated on concrete classes, not on the base type, so generic helpers cannot be implemented without reflection or static abstract interface members. |
| DLL vs generated API boundary | Anything requiring generated lookup methods must be generated. Anything that would make sense as a normal library API without source generation can be compiled into the DLL. |
| Nullable converter shape | Non-null converters (`ValueConverter<TEnum, TProvider>`). EF Core handles null lifting automatically for nullable properties. |
| ValueComparer generation | Always generate for every opted-in enum, unconditionally. |
| Both converter modes per enum | Always generate both ByValue and ByName converters regardless of attribute default, to support per-property overrides. |
| Convention file when no enums exist | Always emit `ConfigureOptimizedEnums()` with an empty body. |
| Abstract class with attribute | OE3004 build error. |
| Nested types | Fully supported, following STJ generator pattern. |
| Extension class naming collision | Namespace-qualify with underscores: `MyApp_Domain_OrderStatusEfCoreExtensions`. |
| Convention registration | Register converter + comparer together via `HaveConversion<T>().HaveValueComparer<T>()`. |
| String-valued enum with ByValue | No special-case. Emitted like any other TValue. |
| Internal generator error diagnostic | OE9003 (aligns with STJ's OE9002, not OE3999 as originally specified). |
| EF Core baseline version | Pin to EF Core 9 in `Directory.Packages.props`. |
| Test layout | Single project with `GeneratorTests/`, `IntegrationTests/`, `Snapshots/` subdirectories. |
| Integration test providers | InMemory for conversion/null tests; Testcontainers+PostgreSQL (`Testcontainers.PostgreSql` + `Npgsql.EntityFrameworkCore.PostgreSQL`) for relational/schema tests (PK/FK/index). |

## Open Implementation Questions

These are engineering validation points to resolve during implementation.

### EF API common denominator

Confirm the exact `ModelConfigurationBuilder`, `PropertyBuilder`, `HaveConversion`, and `HaveValueComparer` signatures that compile cleanly across EF Core 8, 9, and 10. The EF Core 9 baseline simplifies this but cross-version behavior should still be validated.

### Nullable enum property builder overloads

Confirm the correct API shape for `PropertyBuilder<TEnum?>` overloads of the enum-specific extension methods. EF Core's null lifting handles the converter for nullable properties, but the `PropertyBuilder<TEnum?>` type needs to be confirmed to chain correctly with `HasConversion` / `HasComparer`.

## Suggested Implementation Order

1. create project skeleton and packaging
2. inject attribute and implement syntax discovery
3. add diagnostics and snapshot tests for invalid inputs
4. generate comparer + `ByValue` and `ByName` converters
5. generate enum-specific property APIs
6. generate generic property APIs
7. generate `ConfigureOptimizedEnums()` global convention hook
8. add EF runtime/integration tests
9. update README and docs
10. run full build and test validation

## Appendix: Reference Examples

### Example enum

```csharp
using LayeredCraft.OptimizedEnums;
using LayeredCraft.OptimizedEnums.EFCore;

namespace MyApp.Domain;

[OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

### Example entity configuration with global conventions

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder builder)
{
    builder.ConfigureOptimizedEnums();
}
```

### Example property override

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(x => x.Status)
        .HasOptimizedEnumConversionByName();
}
```

### Example enum-specific override

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(x => x.Status)
        .HasOrderStatusConversionByValue();
}
```
