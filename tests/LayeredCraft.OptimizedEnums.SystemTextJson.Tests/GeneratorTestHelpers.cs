using System.Text.RegularExpressions;
using Basic.Reference.Assemblies;
using LayeredCraft.OptimizedEnums;
using LayeredCraft.OptimizedEnums.SystemTextJson.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Tests;

internal class VerifyTestOptions
{
    internal required string SourceCode { get; init; }
    internal string CodePath { get; init; } = "Program.cs";
    internal LanguageVersion LanguageVersion { get; init; } = LanguageVersion.CSharp13;
    internal string AssemblyName { get; init; } = "TestsAssembly";
    internal string? ExpectedDiagnosticId { get; init; }
    internal int? ExpectedTrees { get; init; }
}

internal static class GeneratorTestHelpers
{
    internal static Task Verify(VerifyTestOptions options, CancellationToken cancellationToken = default)
    {
        var (driver, originalCompilation) = GenerateFromSource(options, cancellationToken);

        var result = driver.GetRunResult();

        result.Diagnostics
            .Should()
            .BeEmpty(
                "code should be generated without errors, but found:\n"
                + string.Join("\n---\n", result.Diagnostics.Select(e => $"  - {e.Id}: {e.GetMessage()} at {e.Location}")));

        var parseOptions = originalCompilation.SyntaxTrees.First().Options;
        var reparsedTrees = result.GeneratedTrees
            .Select(tree => CSharpSyntaxTree.ParseText(tree.GetText(), (CSharpParseOptions)parseOptions))
            .ToArray();

        var outputCompilation = originalCompilation.AddSyntaxTrees(reparsedTrees);
        var errors = outputCompilation
            .GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            "generated code should compile without errors, but found:\n"
            + string.Join("\n---\n", errors.Select(e => $"  - {e.Id}: {e.GetMessage()} at {e.Location}")));

        if (options.ExpectedTrees is not null)
            result.GeneratedTrees.Length.Should().Be(options.ExpectedTrees);

        return Verifier
            .Verify(driver)
            .UseDirectory("Snapshots")
            .DisableDiff()
            .ScrubLinesWithReplace(line =>
            {
                if (line.Contains("global::System.CodeDom.Compiler.GeneratedCode"))
                    return RegexHelper.GeneratedCodeAttributeRegex().Replace(line, "REPLACED");
                return line;
            });
    }

    internal static Task VerifyFailure(VerifyTestOptions options, CancellationToken cancellationToken = default)
    {
        var (driver, _) = GenerateFromSource(options, cancellationToken);

        var result = driver.GetRunResult();

        result.Diagnostics.Should().NotBeEmpty("expected diagnostic errors to be generated");

        if (options.ExpectedDiagnosticId is not null)
        {
            result.Diagnostics
                .Should()
                .Contain(
                    d => d.Id == options.ExpectedDiagnosticId,
                    $"expected diagnostic {options.ExpectedDiagnosticId} to be present, but found:\n"
                    + string.Join("\n---\n", result.Diagnostics.Select(e => $"  - {e.Id}: {e.GetMessage()} at {e.Location}")));
        }

        return Verifier
            .Verify(driver)
            .UseDirectory("Snapshots")
            .DisableDiff()
            .ScrubLinesWithReplace(line =>
            {
                if (line.Contains("global::System.CodeDom.Compiler.GeneratedCode"))
                    return RegexHelper.GeneratedCodeAttributeRegex().Replace(line, "REPLACED");
                return line;
            });
    }

    private static (GeneratorDriver driver, Compilation compilation) GenerateFromSource(
        VerifyTestOptions options,
        CancellationToken cancellationToken = default)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(options.LanguageVersion);

        var syntaxTree = CSharpSyntaxTree.ParseText(
            options.SourceCode,
            parseOptions,
            options.CodePath,
            cancellationToken: cancellationToken);

        List<MetadataReference> references =
        [
#if NET10_0_OR_GREATER
            .. Net100.References.All.ToList(),
#elif NET9_0
            .. Net90.References.All.ToList(),
#else
            .. Net80.References.All.ToList(),
#endif
            MetadataReference.CreateFromFile(typeof(OptimizedEnum<,>).Assembly.Location),
        ];

        var compilation = CSharpCompilation.Create(
            options.AssemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        // Run both generators: the main one produces FromName/FromValue,
        // the STJ one produces the JsonConverter.
        // Pass parseOptions so post-init output uses the same language version as the compilation.
        var driver = CSharpGeneratorDriver.Create(
            generators:
            [
                new LayeredCraft.OptimizedEnums.Generator.OptimizedEnumGenerator().AsSourceGenerator(),
                new OptimizedEnumJsonConverterGenerator().AsSourceGenerator(),
            ],
            parseOptions: parseOptions);

        var updatedDriver = driver.RunGenerators(compilation, cancellationToken);

        return (updatedDriver, compilation);
    }
}

internal static partial class RegexHelper
{
    [GeneratedRegex("""(\d+\.\d+\.\d+\.\d+)""", RegexOptions.None, "en-US")]
    internal static partial Regex GeneratedCodeAttributeRegex();
}
