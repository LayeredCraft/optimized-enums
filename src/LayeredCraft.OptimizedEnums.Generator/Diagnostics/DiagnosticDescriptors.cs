using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string UsageCategory = "OptimizedEnums.Usage";

    internal static readonly DiagnosticDescriptor MustBePartial = new(
        "OE0001",
        "OptimizedEnum class must be partial",
        "The class '{0}' must be declared as partial for OptimizedEnum source generation",
        UsageCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NoMembersFound = new(
        "OE0004",
        "No enum members found",
        "The class '{0}' has no public static readonly fields of its own type",
        UsageCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor DuplicateValue = new(
        "OE0005",
        "Duplicate enum value",
        "The class '{0}' has duplicate value on fields '{1}' and '{2}'",
        UsageCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor DuplicateName = new(
        "OE0006",
        "Duplicate enum member name",
        "The class '{0}' has a duplicate member name '{1}'",
        UsageCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NonPrivateConstructor = new(
        "OE0101",
        "OptimizedEnum constructor should be private",
        "The class '{0}' has a non-private constructor; OptimizedEnum constructors should be private to prevent direct instantiation",
        UsageCategory,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NonReadonlyField = new(
        "OE0102",
        "OptimizedEnum static field should be readonly",
        "The field '{0}' in class '{1}' is a public static field of the enum type but is not readonly",
        UsageCategory,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
