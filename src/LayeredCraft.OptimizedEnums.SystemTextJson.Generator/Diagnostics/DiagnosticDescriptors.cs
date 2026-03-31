using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.SystemTextJson.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "OptimizedEnums.SystemTextJson";

    internal static readonly DiagnosticDescriptor MustInheritOptimizedEnum = new(
        "OE2001",
        "OptimizedEnumJsonConverter requires an OptimizedEnum subclass",
        "The class '{0}' must inherit from OptimizedEnum<TEnum, TValue> to use [OptimizedEnumJsonConverter]",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MustBePartial = new(
        "OE2002",
        "OptimizedEnum class must be partial for JSON converter generation",
        "The class '{0}' must be declared as partial for [OptimizedEnumJsonConverter] source generation",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
