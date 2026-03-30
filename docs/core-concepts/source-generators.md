# Source Generators

## What Is an Incremental Source Generator?

Roslyn source generators are compiler extensions that participate in the build and emit additional source files. The `IIncrementalGenerator` API (introduced in .NET 6 SDK) provides a pipeline model where intermediate results are cached and only re-executed when their inputs change.

`LayeredCraft.OptimizedEnums` uses this API to ensure that:

- The generator only re-runs for classes that actually changed
- No extra allocations or work occur on incremental builds
- The IDE design-time build (IntelliSense) stays fast

## Trigger Condition

The generator activates for any class declaration that:

1. Has a base list (syntax-level filter — very cheap)
2. Inherits from `OptimizedEnum<TEnum, TValue>` anywhere in its chain (semantic check)

No attribute is required. Inheriting the base class is sufficient signal.

## Incremental Caching

The generator builds an `EnumInfo` record from the semantic model. This record uses value-equality (`EquatableArray<T>` for collections) so Roslyn can cache it between builds. If nothing in your enum class changed, the emit step is skipped entirely.

## Template Engine

The code generation step uses **Scriban** — a fast, lightweight template engine. The template is embedded directly in the generator DLL at build time (`PackageScribanIncludeSource=true`), so no external files are needed at runtime.

The template lives at `src/LayeredCraft.OptimizedEnums.Generator/Templates/OptimizedEnum.scriban` and produces a single `partial class` file per enum.

## Build Output

Generated files appear under your project's `obj/` directory:

```
obj/Debug/net9.0/generated/
  LayeredCraft.OptimizedEnums.Generator/
    LayeredCraft.OptimizedEnums.Generator.OptimizedEnumGenerator/
      OrderStatus.g.cs
```

You can inspect these files to understand exactly what was generated. They are also visible in your IDE via "Go to definition" on any generated method.

!!! note "Source generators and NuGet"
    The generator DLL is delivered inside the NuGet package under `analyzers/dotnet/cs/`. It is loaded by the compiler host, not referenced as a normal assembly. This is why the package has `PrivateAssets="all"` on its analyzer references internally.
