# AOT & Trimming

## Overview

`LayeredCraft.OptimizedEnums` is designed from the ground up for AOT compilation and aggressive trimming. There is no reflection at runtime — all lookup tables are generated at compile time.

## AOT Compatibility

The library is compatible with:

- **NativeAOT** (`PublishAot=true`)
- **ReadyToRun** (`PublishReadyToRun=true`)
- **.NET trimming** (`PublishTrimmed=true`)
- **Blazor WebAssembly** (AOT mode)
- **AWS Lambda self-contained deployments**

## Why It Works

Traditional SmartEnum implementations use `FieldInfo.GetValue()` or `Activator.CreateInstance()` to discover members — both of which are either blocked or stripped by the trimmer. This library generates explicit code that directly references the field values by name, which the trimmer can analyze statically.

Generated code example:

```csharp
// The trimmer can see exactly which fields are referenced
private static readonly global::System.Collections.Generic.Dictionary<string, global::MyApp.OrderStatus> s_byName =
    new(global::System.StringComparer.Ordinal)
    {
        ["Pending"] = global::MyApp.OrderStatus.Pending,
        ["Paid"]    = global::MyApp.OrderStatus.Paid,
        ["Shipped"] = global::MyApp.OrderStatus.Shipped,
    };
```

Every reference is a direct field access. The trimmer retains exactly what is needed.

## Lambda / Serverless

This library was designed with Lambda cold starts in mind. Because `Count` is a compile-time constant and all lookup dictionaries are lazily initialized on first type access (not per-call), the initialization cost is paid once and is minimal.

For AOT Lambda deployments (`PublishAot=true`), the library works without any additional trimmer annotations or `DynamicDependency` attributes.

## Testing AOT Compatibility

You can verify your enum works under trimming by publishing with:

```bash
dotnet publish -c Release -r linux-x64 /p:PublishAot=true
```

If any reflection-based code sneaks in via a dependency, the trimmer will warn. `LayeredCraft.OptimizedEnums` itself produces no trimmer warnings.
