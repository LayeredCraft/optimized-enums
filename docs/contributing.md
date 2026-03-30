# Contributing

Contributions are welcome. Please open an issue or pull request on [GitHub](https://github.com/layeredcraft/optimized-enums).

## Development Setup

1. Clone the repository
2. Ensure .NET 10 SDK is installed (`global.json` pins the version)
3. Open `LayeredCraft.OptimizedEnums.slnx` in your IDE

## Running Tests

```bash
# Runtime tests
dotnet test --project tests/LayeredCraft.OptimizedEnums.Tests/LayeredCraft.OptimizedEnums.Tests.csproj

# Generator snapshot tests
dotnet test --project tests/LayeredCraft.OptimizedEnums.Generator.Tests/LayeredCraft.OptimizedEnums.Generator.Tests.csproj
```

## Running Benchmarks

```bash
cd tests/LayeredCraft.OptimizedEnums.Benchmarks
dotnet run -c Release -- --filter '*'
```

## Snapshot Tests

The generator tests use [Verify.SourceGenerators](https://github.com/VerifyTests/Verify) for snapshot testing. If you change generated output, update the snapshots:

```bash
dotnet test ... -- --update-snapshots
```

Or delete the relevant `.verified.cs` file and let the test re-create it.

## Building the Package Locally

```bash
bash scripts/pack-local.sh
```

This increments a local counter and publishes to `/usr/local/share/nuget/local/`.

## Code Style

- Follow existing patterns in the codebase
- Generator code uses C# latest features (`field` keyword, `extension()` blocks, pattern matching)
- Runtime code targets `netstandard2.0` — avoid APIs not available there

## License

By contributing, you agree that your contributions will be licensed under the MIT license.
