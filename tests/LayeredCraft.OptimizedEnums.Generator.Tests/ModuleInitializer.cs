using System.Runtime.CompilerServices;

namespace LayeredCraft.OptimizedEnums.Generator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
