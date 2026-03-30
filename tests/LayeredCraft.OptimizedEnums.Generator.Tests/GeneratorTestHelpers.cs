using System.Text.RegularExpressions;
using Basic.Reference.Assemblies;
using LayeredCraft.OptimizedEnums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayeredCraft.OptimizedEnums.Generator.Tests;

/// <summary>
/// Extends <see cref="CodeGenerationOptions"/> with additional options for verification testing.
/// </summary>
internal class VerifyTestOptions : CodeGenerationOptions
{
    /// <summary>Gets or initializes the expected number of syntax trees to be generated.</summary>
    internal int? ExpectedTrees { get; init; } = null;

    /// <summary>Gets or initializes the expected diagnostic ID for failure tests.</summary>
    internal string? ExpectedDiagnosticId { get; init; } = null;
}

/// <summary>Configuration options for code generation testing.</summary>
internal class CodeGenerationOptions
{
    /// <summary>Gets or initializes the source code to compile and test.</summary>
    internal required string SourceCode { get; init; }

    /// <summary>Gets or initializes the file path for the source code.</summary>
    internal string CodePath { get; init; } = "Program.cs";

    /// <summary>Gets or initializes the C# language version to use for compilation.</summary>
    internal LanguageVersion LanguageVersion { get; init; } = LanguageVersion.CSharp13;

    /// <summary>Gets or initializes optional diagnostics to suppress during compilation.</summary>
    internal Dictionary<string, ReportDiagnostic>? DiagnosticsToSuppress { get; init; } = null;

    /// <summary>Gets or initializes the name of the test assembly.</summary>
    internal string AssemblyName { get; init; } = "TestsAssembly";
}

internal static class GeneratorTestHelpers
{
    internal static Task Verify(
        VerifyTestOptions options,
        CancellationToken cancellationToken = default)
    {
        var (driver, originalCompilation) = GenerateFromSource(options, cancellationToken);

        driver.Should().NotBeNull();

        var result = driver.GetRunResult();

        result.Diagnostics
            .Should()
            .BeEmpty(
                "code should be generated without errors, but found:\n"
                + string.Join(
                    "\n---\n",
                    result.Diagnostics.Select(e => $"  - {e.Id}: {e.GetMessage()} at {e.Location}")));

        // Reparse generated trees with the same parse options to ensure consistent features
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
            + string.Join(
                "\n---\n",
                errors.Select(e => $"  - {e.Id}: {e.GetMessage()} at {e.Location}")));

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

    internal static Task VerifyFailure(
        VerifyTestOptions options,
        CancellationToken cancellationToken = default)
    {
        var (driver, _) = GenerateFromSource(options, cancellationToken);

        driver.Should().NotBeNull();

        var result = driver.GetRunResult();

        result.Diagnostics
            .Should()
            .NotBeEmpty("expected diagnostic errors to be generated");

        if (options.ExpectedDiagnosticId is not null)
        {
            result.Diagnostics
                .Should()
                .Contain(
                    d => d.Id == options.ExpectedDiagnosticId,
                    $"expected diagnostic {options.ExpectedDiagnosticId} to be present, but found:\n"
                    + string.Join(
                        "\n---\n",
                        result.Diagnostics.Select(e => $"  - {e.Id}: {e.GetMessage()} at {e.Location}")));
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

    internal static (GeneratorDriver driver, Compilation compilation) GenerateFromSource(
        CodeGenerationOptions options,
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

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable)
            .WithSpecificDiagnosticOptions(options.DiagnosticsToSuppress);

        var compilation = CSharpCompilation.Create(
            options.AssemblyName,
            [syntaxTree],
            references,
            compilationOptions);

        var generator = new OptimizedEnumGenerator().AsSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var updatedDriver = driver.RunGenerators(compilation, cancellationToken);

        return (updatedDriver, compilation);
    }
}

internal static partial class RegexHelper
{
    [GeneratedRegex("""(\d+\.\d+\.\d+\.\d+)""", RegexOptions.None, "en-US")]
    internal static partial Regex GeneratedCodeAttributeRegex();
}
