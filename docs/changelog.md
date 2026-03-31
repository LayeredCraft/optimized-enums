# Changelog

All notable changes to this project will be documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- `LayeredCraft.OptimizedEnums.SystemTextJson` package — source-generated, zero-reflection `JsonConverter` support
  - `[OptimizedEnumJsonConverter]` attribute with `ByName` and `ByValue` strategies
  - Emits a concrete non-generic `JsonConverter<T>` for each decorated class — no runtime reflection, full AOT/NativeAOT compatibility
  - Stamps `[JsonConverter(typeof(...))]` on a generated partial class stub — no manual `JsonSerializerOptions` registration required
  - Declares `LayeredCraft.OptimizedEnums` as a NuGet dependency — only one package reference needed
  - `OE2001` diagnostic for classes not inheriting `OptimizedEnum<TEnum, TValue>`
  - `OE2002` diagnostic for classes missing `partial`
- `OptimizedEnum<TEnum>` single-parameter base class for `int`-valued enums
- Inheritance-based generation trigger — `[OptimizedEnum]` attribute no longer required
- `Microsoft.CSharp.dll` bundled in `analyzers/dotnet/cs/` for Scriban dynamic dispatch
- `GetDependencyTargetPaths` MSBuild target for IDE analyzer resolution

### Changed
- Generator now triggers on `OptimizedEnum<TEnum, TValue>` inheritance rather than `[OptimizedEnum]` attribute
- `OE0001` base-type check moved before partial check — unrelated classes no longer receive false diagnostics

### Removed
- `OptimizedEnumAttribute` — no longer needed
- `OE0002` (must be sealed) — `sealed` is now optional
- `OE0003` (must inherit) — superseded by inheritance-based triggering
