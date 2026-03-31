# JSON Serialization (System.Text.Json)

The `LayeredCraft.OptimizedEnums.SystemTextJson` package adds source-generated, zero-reflection `JsonConverter` support for your OptimizedEnum types. Decorate a class with `[OptimizedEnumJsonConverter]` and the generator emits a concrete converter and wires it up via `[JsonConverter]` automatically — no factory, no runtime type-checking, full AOT compatibility.

## Installation

Install the SystemTextJson package. The core `LayeredCraft.OptimizedEnums` package is pulled in automatically as a dependency — only one `dotnet add` is needed:

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

## The Attribute

Two serialization strategies are available, controlled by the `OptimizedEnumJsonConverterType` enum:

| Strategy | Value | JSON representation | Deserialization input |
|---|---|---|---|
| `ByName` | `0` | `"Pending"` (the Name string) | JSON string |
| `ByValue` | `1` | `1` (the underlying Value) | JSON number / string / bool depending on TValue |

Apply the attribute to your OptimizedEnum class:

```csharp
using LayeredCraft.OptimizedEnums;
using LayeredCraft.OptimizedEnums.SystemTextJson;

[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

That is all the user code required. The generator handles everything else.

## What Gets Generated

For the `ByName` example above, the generator emits two things into a single `.g.cs` file:

**1. A partial class stub stamped with `[JsonConverter]`:**

```csharp
[JsonConverter(typeof(OrderStatusNameJsonConverter))]
partial class OrderStatus { }
```

This is how System.Text.Json discovers the converter — the attribute is on the type itself, so no manual registration in `JsonSerializerOptions` is ever needed.

**2. A concrete, non-generic converter:**

```csharp
[GeneratedCode(...)]
internal sealed class OrderStatusNameJsonConverter
    : JsonConverter<global::MyApp.Domain.OrderStatus>
{
    public override OrderStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(...);

        var name = reader.GetString()!;
        if (!OrderStatus.TryFromName(name, out var result))
            throw new JsonException($"'{name}' is not a valid name for OrderStatus.");

        return result!;
    }

    public override void Write(
        Utf8JsonWriter writer,
        OrderStatus value,
        JsonSerializerOptions options)
        => writer.WriteStringValue(value.Name);
}
```

## ByName Strategy

Serializes using the member's **Name** string. Suitable when your JSON needs to be human-readable or stable across value changes.

```csharp
[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

```json
{ "status": "Pending" }
```

Deserialization calls `TryFromName` with Ordinal string comparison (the same as the hand-written lookup tables). An unrecognised name throws `JsonException`.

## ByValue Strategy

Serializes using the member's **Value**. Suitable for compact payloads or when matching external integer/string codes.

```csharp
[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByValue)]
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int> { ... }
```

```json
{ "status": 1 }
```

Deserialization delegates to `JsonSerializer.Deserialize<TValue>` for the raw value, then calls `TryFromValue`. An unrecognised value throws `JsonException`.

## String-Valued Enums

Both strategies work with any `TValue`, including `string`:

```csharp
[OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByValue)]
public sealed partial class Color : OptimizedEnum<Color, string>
{
    public static readonly Color Red   = new("red",   nameof(Red));
    public static readonly Color Green = new("green", nameof(Green));
    public static readonly Color Blue  = new("blue",  nameof(Blue));

    private Color(string value, string name) : base(value, name) { }
}
```

With `ByValue`, the JSON value is `"red"`/`"green"`/`"blue"`. With `ByName`, it is `"Red"`/`"Green"`/`"Blue"`.

## AOT and Trimming Safety

Because the generator emits a concrete, non-generic converter for each type, there is no runtime reflection anywhere in the deserialization path:

- No `Activator.CreateInstance`
- No `MakeGenericType`
- No `Delegate.CreateDelegate`
- `[JsonConverter]` is stamped on the partial class at compile time, so STJ's source-gen pipeline sees it

The generated converter calls `TryFromName` / `TryFromValue` directly — methods that are themselves source-generated static dictionary lookups.

## Diagnostics

The SystemTextJson generator emits its own diagnostics with the `OE2xxx` prefix. See [Diagnostics](../advanced/diagnostics.md#systemtextjson-diagnostics) for details.

## Constraints

- The class must inherit from `OptimizedEnum<TEnum, TValue>` (OE2001).
- The class must be declared `partial` (OE2002).
- Only one `[OptimizedEnumJsonConverter]` per class (enforced by `AllowMultiple = false` on the attribute and by `[JsonConverter]` itself).
