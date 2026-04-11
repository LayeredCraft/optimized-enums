using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.EFCore.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "OptimizedEnums.EFCore";

    internal static readonly DiagnosticDescriptor MustInheritOptimizedEnum = new(
        "OE3001",
        "OptimizedEnumEfCore requires an OptimizedEnum subclass",
        "The class '{0}' must inherit from OptimizedEnum<TEnum, TValue> to use [OptimizedEnumEfCore]",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MustBePartial = new(
        "OE3002",
        "OptimizedEnum class must be partial for EF Core generation",
        "The class '{0}' must be declared as partial for [OptimizedEnumEfCore] source generation",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor UnknownStorageType = new(
        "OE3003",
        "Unknown OptimizedEnumEfCoreStorage value",
        "The class '{0}' specifies an unknown OptimizedEnumEfCoreStorage value '{1}'; valid values are ByValue (0) and ByName (1)",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor UnsupportedTarget = new(
        "OE3004",
        "Unsupported EF Core target usage",
        "{0}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor GeneratorInternalError = new(
        "OE9003",
        "OptimizedEnums EFCore generator internal error",
        "An unexpected error occurred while generating the EF Core support for '{0}': {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
