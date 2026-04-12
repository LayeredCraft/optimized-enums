namespace LayeredCraft.OptimizedEnums.EFCore.Tests.GeneratorTests;

public class GeneratorVerifyTests
{
    [Fact]
    public async Task ByValue_WithNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Paid    = new(2, nameof(Paid));
                        public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                // core enum .g.cs + EFCore per-enum .g.cs + attribute .g.cs + conventions .g.cs
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByName_WithNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Paid    = new(2, nameof(Paid));
                        public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByValue_GlobalNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
                    public sealed partial class Priority : OptimizedEnum<Priority, int>
                    {
                        public static readonly Priority Low    = new(1, nameof(Low));
                        public static readonly Priority Medium = new(2, nameof(Medium));
                        public static readonly Priority High   = new(3, nameof(High));

                        private Priority(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByName_GlobalNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
                    public sealed partial class Priority : OptimizedEnum<Priority, int>
                    {
                        public static readonly Priority Low    = new(1, nameof(Low));
                        public static readonly Priority Medium = new(2, nameof(Medium));
                        public static readonly Priority High   = new(3, nameof(High));

                        private Priority(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByValue_StringValueType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
                    public sealed partial class Color : OptimizedEnum<Color, string>
                    {
                        public static readonly Color Red   = new("red",   nameof(Red));
                        public static readonly Color Green = new("green", nameof(Green));
                        public static readonly Color Blue  = new("blue",  nameof(Blue));

                        private Color(string value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByName_StringValueType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByName)]
                    public sealed partial class Color : OptimizedEnum<Color, string>
                    {
                        public static readonly Color Red   = new("red",   nameof(Red));
                        public static readonly Color Green = new("green", nameof(Green));
                        public static readonly Color Blue  = new("blue",  nameof(Blue));

                        private Color(string value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task NestedType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    public partial class Outer
                    {
                        [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
                        public sealed partial class Status : OptimizedEnum<Status, int>
                        {
                            public static readonly Status Active   = new(1, nameof(Active));
                            public static readonly Status Inactive = new(2, nameof(Inactive));

                            private Status(int value, string name) : base(value, name) { }
                        }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task IntermediateAbstractBase() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    public abstract partial class OrderStatusBase<TEnum> : OptimizedEnum<TEnum, int>
                        where TEnum : OptimizedEnum<TEnum, int>
                    {
                        protected OrderStatusBase(int value, string name) : base(value, name) { }
                    }

                    [OptimizedEnumEfCore(OptimizedEnumEfCoreStorage.ByValue)]
                    public sealed partial class OrderStatus : OrderStatusBase<OrderStatus>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Paid    = new(2, nameof(Paid));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 4,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_NotOptimizedEnum() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore]
                    public sealed partial class NotAnEnum
                    {
                    }
                    """,
                ExpectedDiagnosticId = "OE3001",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_NotPartial() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore]
                    public sealed class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE3002",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_UnknownStorageType() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore((OptimizedEnumEfCoreStorage)99)]
                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE3003",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_AbstractClass() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.EFCore;

                    namespace MyApp.Domain;

                    [OptimizedEnumEfCore]
                    public abstract partial class OrderStatusBase : OptimizedEnum<OrderStatusBase, int>
                    {
                        protected OrderStatusBase(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE3004",
            },
            TestContext.Current.CancellationToken);
}
