using Microsoft.CodeAnalysis;

namespace LayeredCraft.OptimizedEnums.Generator.Tests;

public class GeneratorVerifyTests
{
    [Fact]
    public async Task SimpleEnum_WithNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Paid = new(2, nameof(Paid));
                        public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task SimpleEnum_GlobalNamespace() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    public sealed partial class Priority : OptimizedEnum<Priority, int>
                    {
                        public static readonly Priority Low = new(1, nameof(Low));
                        public static readonly Priority Medium = new(2, nameof(Medium));
                        public static readonly Priority High = new(3, nameof(High));

                        private Priority(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task StringValueType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class Color : OptimizedEnum<Color, string>
                    {
                        public static readonly Color Red = new("red", nameof(Red));
                        public static readonly Color Green = new("green", nameof(Green));
                        public static readonly Color Blue = new("blue", nameof(Blue));

                        private Color(string value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task MultipleMembers() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class DayOfWeek : OptimizedEnum<DayOfWeek, int>
                    {
                        public static readonly DayOfWeek Monday = new(1, nameof(Monday));
                        public static readonly DayOfWeek Tuesday = new(2, nameof(Tuesday));
                        public static readonly DayOfWeek Wednesday = new(3, nameof(Wednesday));
                        public static readonly DayOfWeek Thursday = new(4, nameof(Thursday));
                        public static readonly DayOfWeek Friday = new(5, nameof(Friday));
                        public static readonly DayOfWeek Saturday = new(6, nameof(Saturday));
                        public static readonly DayOfWeek Sunday = new(7, nameof(Sunday));

                        private DayOfWeek(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_NotPartial() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE0001",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Error_NoMembers() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE0004",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task NestedType() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public partial class Outer
                    {
                        public sealed partial class Status : OptimizedEnum<Status, int>
                        {
                            public static readonly Status Active = new(1, nameof(Active));
                            public static readonly Status Inactive = new(2, nameof(Inactive));

                            private Status(int value, string name) : base(value, name) { }
                        }
                    }
                    """,
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task SameClassName_DifferentNamespaces() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain1
                    {
                        public sealed partial class Status : OptimizedEnum<Status, int>
                        {
                            public static readonly Status Active = new(1, nameof(Active));
                            private Status(int value, string name) : base(value, name) { }
                        }
                    }

                    namespace MyApp.Domain2
                    {
                        public sealed partial class Status : OptimizedEnum<Status, int>
                        {
                            public static readonly Status Active = new(1, nameof(Active));
                            private Status(int value, string name) : base(value, name) { }
                        }
                    }
                    """,
                ExpectedTrees = 2,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Warning_NonPrivateConstructor() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));

                        public OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                DiagnosticsToSuppress = new Dictionary<string, ReportDiagnostic>
                {
                    ["OE0101"] = ReportDiagnostic.Suppress,
                },
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Warning_OE0101_NonPrivateConstructor_IsEmitted() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));

                        public OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE0101",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Warning_OE0102_NonReadonlyField_IsEmitted() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static OrderStatus NonReadonly = new(2, nameof(NonReadonly));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE0102",
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task AbstractBase_WithCRTP() =>
        await GeneratorTestHelpers.Verify(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public abstract partial class DomainEnum<TSelf> : OptimizedEnum<TSelf, int>
                        where TSelf : DomainEnum<TSelf>
                    {
                        public string Description { get; }

                        protected DomainEnum(int value, string name, string description)
                            : base(value, name)
                        {
                            Description = description;
                        }
                    }

                    public sealed partial class OrderStatus : DomainEnum<OrderStatus>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending), "Order is pending");
                        public static readonly OrderStatus Paid    = new(2, nameof(Paid),    "Payment received");

                        private OrderStatus(int value, string name, string description)
                            : base(value, name, description) { }
                    }
                    """,
                ExpectedTrees = 1,
            },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task AbstractBase_Alone_ProducesNoOutput()
    {
        var options = new VerifyTestOptions
        {
            SourceCode = """
                using LayeredCraft.OptimizedEnums;

                namespace MyApp.Domain;

                public abstract partial class DomainEnum<TSelf> : OptimizedEnum<TSelf, int>
                    where TSelf : DomainEnum<TSelf>
                {
                    public string Description { get; }

                    protected DomainEnum(int value, string name, string description)
                        : base(value, name)
                    {
                        Description = description;
                    }
                }
                """,
            ExpectedTrees = 0,
        };

        var (driver, _) = GeneratorTestHelpers.GenerateFromSource(options, TestContext.Current.CancellationToken);

        var result = driver.GetRunResult();

        result.Diagnostics.Should().BeEmpty("abstract base class alone should produce no diagnostics");
        result.GeneratedTrees.Length.Should().Be(0, "abstract base class alone should produce no generated output");
    }

    [Fact]
    public async Task Error_OE0005_DuplicateValue_IsEmitted() =>
        await GeneratorTestHelpers.VerifyFailure(
            new VerifyTestOptions
            {
                SourceCode = """
                    using LayeredCraft.OptimizedEnums;

                    namespace MyApp.Domain;

                    public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
                    {
                        public static readonly OrderStatus Pending = new(1, nameof(Pending));
                        public static readonly OrderStatus Duplicate = new(1, nameof(Duplicate));

                        private OrderStatus(int value, string name) : base(value, name) { }
                    }
                    """,
                ExpectedDiagnosticId = "OE0005",
            },
            TestContext.Current.CancellationToken);
}
