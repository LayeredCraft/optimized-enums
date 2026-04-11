# Installation

## Requirements

- .NET 8.0, .NET 9.0, or .NET 10.0 (or any target that supports `netstandard2.0` for the runtime)
- C# 9.0 or later (for `partial` class support)

## NuGet Package

Add the package to your project:

=== ".NET CLI"

    ```bash
    dotnet add package LayeredCraft.OptimizedEnums
    ```

=== "Package Manager"

    ```powershell
    Install-Package LayeredCraft.OptimizedEnums
    ```

=== "PackageReference"

    ```xml
    <PackageReference Include="LayeredCraft.OptimizedEnums" Version="x.x.x" />
    ```

## What Gets Installed

The package bundles two assemblies:

- **`LayeredCraft.OptimizedEnums.Generator.dll`** — the Roslyn source generator, loaded by the compiler
- **`LayeredCraft.OptimizedEnums.dll`** — the runtime base classes (`OptimizedEnum<TEnum, TValue>`, `OptimizedEnum<TEnum>`)

Both are delivered automatically by the single NuGet package. No separate runtime package reference is needed.

## Optional: JSON Serialization

To add source-generated `System.Text.Json` converter support, install the SystemTextJson package. It declares the core package as a dependency, so only one `dotnet add` is needed:

=== ".NET CLI"

    ```bash
    dotnet add package LayeredCraft.OptimizedEnums.SystemTextJson
    ```

=== "Package Manager"

    ```powershell
    Install-Package LayeredCraft.OptimizedEnums.SystemTextJson
    ```

=== "PackageReference"

    ```xml
    <PackageReference Include="LayeredCraft.OptimizedEnums.SystemTextJson" Version="x.x.x" />
    ```

See [JSON Serialization](../usage/json-serialization.md) for usage details.

## Optional: Entity Framework Core

To add source-generated EF Core value converter support, install the EFCore package. It declares the core package as a dependency:

=== ".NET CLI"

    ```bash
    dotnet add package LayeredCraft.OptimizedEnums.EFCore
    ```

=== "Package Manager"

    ```powershell
    Install-Package LayeredCraft.OptimizedEnums.EFCore
    ```

=== "PackageReference"

    ```xml
    <PackageReference Include="LayeredCraft.OptimizedEnums.EFCore" Version="x.x.x" />
    ```

Supports EF Core 8, 9, and 10. See [Entity Framework Core](../usage/ef-core.md) for usage details.

## Verifying the Installation

After adding the package, define a type that inherits from `OptimizedEnum<TEnum, TValue>` and declare it `partial`. Build the project — the generator runs during compilation and produces the lookup members. You can inspect the generated output under `obj/` or via your IDE's "Go to definition" on any generated method.

```csharp
using LayeredCraft.OptimizedEnums;

public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    private OrderStatus(int value, string name) : base(value, name) { }
}

// If installed correctly, this compiles:
var s = OrderStatus.FromName("Pending");
```

!!! tip
    If `FromName` is not found after adding the package, see [Troubleshooting](../advanced/diagnostics.md).
