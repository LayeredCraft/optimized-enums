namespace LayeredCraft.OptimizedEnums.SystemTextJson.Tests;

public class GeneratorVerifyTests
{
    [Fact]
    public async Task ByName_WithNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Paid    = new(2, nameof(Paid));
                        public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 3,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByValue_WithNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByValue)]
                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Paid    = new(2, nameof(Paid));
                        public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 3,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByName_GlobalNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
                    public sealed partial class Priority : OptimizedEnum<Priority, int>
                    {
                        public static readonly Priority Low    = new(1, nameof(Low));
                        public static readonly Priority Medium = new(2, nameof(Medium));
                        public static readonly Priority High   = new(3, nameof(High));

                        private Priority(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 3,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByName_StringValueType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
                    public sealed partial class Color : OptimizedEnum<Color, string>
                    {
                        public static readonly Color Red   = new("red",   nameof(Red));
                        public static readonly Color Green = new("green", nameof(Green));
                        public static readonly Color Blue  = new("blue",  nameof(Blue));

                        private Color(string value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 3,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByValue_StringValueType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByValue)]
                    public sealed partial class Color : OptimizedEnum<Color, string>
                    {
                        public static readonly Color Red   = new("red",   nameof(Red));
                        public static readonly Color Green = new("green", nameof(Green));
                        public static readonly Color Blue  = new("blue",  nameof(Blue));

                        private Color(string value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 3,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ByName_NestedType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    public partial class Outer
                    {
                        [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
                        public sealed partial class Status : OptimizedEnum<Status, int>
                        {
                            public static readonly Status Active   = new(1, nameof(Active));
                            public static readonly Status Inactive = new(2, nameof(Inactive));

                            private Status(int value, string name) : base(value, name) { }
                        }
                    }
                    """,
                ExpectedTrees = 3,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_NotOptimizedEnum() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
                    public sealed partial class NotAnEnum
                    {
                    }
                    """,
                ExpectedDiagnosticId = "OE2001",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_NotPartial() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;
                    using LayeredCraft.OptimizedEnums.SystemTextJson;

                    namespace MyApp.Domain;

                    [OptimizedEnumJsonConverter(OptimizedEnumJsonConverterType.ByName)]
                    public sealed class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE2002",
            },
            TestContext.Current.CancellationToken);
}
