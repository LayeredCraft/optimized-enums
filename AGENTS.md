# AGENTS.md

## Toolchain
- `global.json` pins SDK `10.0.103` and sets the test runner to `Microsoft.Testing.Platform`.
- Test projects use xUnit v3 + MTP, so focused runs do **not** use the usual `dotnet test --filter ...` syntax.

## Commands
- Build everything: `dotnet build LayeredCraft.OptimizedEnums.slnx -v minimal`
- Run all tests: `dotnet test --solution LayeredCraft.OptimizedEnums.slnx -v minimal`
- Run one test project: `dotnet test --project tests/LayeredCraft.OptimizedEnums.Generator.Tests/LayeredCraft.OptimizedEnums.Generator.Tests.csproj`
- Run one xUnit test method: `dotnet test --project tests/LayeredCraft.OptimizedEnums.Generator.Tests/LayeredCraft.OptimizedEnums.Generator.Tests.csproj -- --filter-method "*SimpleEnum_WithNamespace"`
- Build docs: `uv sync --locked --all-extras --dev` then `uv run zensical build --clean`
- Create local NuGet packages: `bash scripts/pack-local.sh`

## Repo Shape
- `src/LayeredCraft.OptimizedEnums` is the runtime library. It targets `netstandard2.0`; keep runtime code compatible with that surface area.
- `src/LayeredCraft.OptimizedEnums.Generator` is the main incremental generator. The real wiring is `OptimizedEnumGenerator` -> `Providers/EnumSyntaxProvider.cs` -> `Emitters/EnumEmitter.cs` -> `Templates/OptimizedEnum.scriban`.
- `src/LayeredCraft.OptimizedEnums.SystemTextJson.Generator` is a separate incremental generator for `[OptimizedEnumJsonConverter]`; it also injects the attribute definition during post-initialization.
- `tests/LayeredCraft.OptimizedEnums.Tests` covers runtime behavior.
- `tests/LayeredCraft.OptimizedEnums.Generator.Tests` and `tests/LayeredCraft.OptimizedEnums.SystemTextJson.Tests` are snapshot-style generator tests using Verify.
- `tests/LayeredCraft.OptimizedEnums.Benchmarks` is BenchmarkDotNet-only and targets `net9.0`.

## Packaging Gotchas
- The publishable NuGet package for the core feature is the generator project `src/LayeredCraft.OptimizedEnums.Generator`, not the runtime project.
- `src/LayeredCraft.OptimizedEnums/LayeredCraft.OptimizedEnums.csproj` is intentionally `IsPackable=false`, uses `PackageId` `LayeredCraft.OptimizedEnums.Core`, and sets `AssemblyName` to `LayeredCraft.OptimizedEnums` because the generator package packs that DLL directly.
- Local packing writes to `/usr/local/share/nuget/local` and increments `scripts/.counter`; do not reset that counter casually.

## Test Notes
- All test projects multi-target `net8.0;net9.0;net10.0`, so a focused test still runs once per target unless you add framework scoping yourself.
- Generator snapshot tests call `VerifySourceGenerators.Initialize()` from `ModuleInitializer.cs`; if snapshot behavior changes, check those initializers first.
