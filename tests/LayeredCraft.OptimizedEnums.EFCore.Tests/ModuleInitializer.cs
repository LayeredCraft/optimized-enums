using System.Runtime.CompilerServices;

namespace LayeredCraft.OptimizedEnums.EFCore.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
