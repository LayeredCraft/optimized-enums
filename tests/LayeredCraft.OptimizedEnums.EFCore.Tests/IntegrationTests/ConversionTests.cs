using Microsoft.EntityFrameworkCore;

namespace LayeredCraft.OptimizedEnums.EFCore.Tests.IntegrationTests;

/// <summary>
/// Conversion, null behavior, and API surface tests using the InMemory provider.
/// </summary>
public class ConversionTests
{
    private static TestDbContext CreateContext(Action<ModelBuilder>? modelConfig = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options, modelConfig);
    }

    // ── ByValue conversion ─────────────────────────────────────────────────

    [Fact]
    public async Task ByValue_SaveAndLoad_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, Status = OrderStatus.Paid });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.Status.Should().Be(OrderStatus.Paid);
    }

    // ── ByName conversion ──────────────────────────────────────────────────

    [Fact]
    public async Task ByName_SaveAndLoad_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, Currency = Currency.Eur });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.Currency.Should().Be(Currency.Eur);
    }

    // ── Global convention hook ─────────────────────────────────────────────

    [Fact]
    public async Task GlobalConvention_AppliesEnumDefault()
    {
        // The convention converters registered in TestDbContext.ConfigureConventions
        // mirror what the generated ConfigureOptimizedEnums() extension does.
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, Status = OrderStatus.Shipped });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.Status.Should().Be(OrderStatus.Shipped);
    }

    // ── Property override supersedes convention ────────────────────────────

    [Fact]
    public async Task PropertyOverride_ByName_SupersedesConvention()
    {
        // Override the convention ByValue converter with an explicit ByName converter
        // on a single property — mirrors what HasOrderStatusConversionByName() would do.
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext(builder =>
        {
            builder.Entity<Order>()
                .Property(x => x.Status)
                .HasConversion(new OrderStatusByNameConverter());
        });
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, Status = OrderStatus.Pending });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.Status.Should().Be(OrderStatus.Pending);
    }

    // ── Nullable properties ────────────────────────────────────────────────

    [Fact]
    public async Task NullableProperty_NullValue_RoundTripsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, OptionalStatus = null });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.OptionalStatus.Should().BeNull();
    }

    [Fact]
    public async Task NullableProperty_NonNullValue_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, OptionalStatus = OrderStatus.Paid });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.OptionalStatus.Should().Be(OrderStatus.Paid);
    }

    // ── Intermediate abstract base ─────────────────────────────────────────

    [Fact]
    public async Task IntermediateAbstractBase_SaveAndLoad_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, ShipmentState = ShipmentState.Delivered });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.ShipmentState.Should().Be(ShipmentState.Delivered);
    }

    // ── Explicit property-level converter helpers ──────────────────────────
    // These verify that attaching a specific converter on a single property
    // works correctly — equivalent to the generated HasXxxConversionByValue/ByName
    // extension methods.

    [Fact]
    public async Task ExplicitPropertyConverter_ByValue_Works()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext(builder =>
        {
            builder.Entity<Order>()
                .Property(x => x.Status)
                .HasConversion(new OrderStatusByValueConverter());
        });
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, Status = OrderStatus.Shipped });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task ExplicitPropertyConverter_ByName_Works()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = CreateContext(builder =>
        {
            builder.Entity<Order>()
                .Property(x => x.Status)
                .HasConversion(new OrderStatusByNameConverter());
        });
        await ctx.Database.EnsureCreatedAsync(ct);

        ctx.Orders.Add(new Order { Id = 1, Status = OrderStatus.Paid });
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Orders.FindAsync([1], ct);
        loaded!.Status.Should().Be(OrderStatus.Paid);
    }
}
