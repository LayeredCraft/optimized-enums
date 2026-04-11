using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LayeredCraft.OptimizedEnums.EFCore.Tests.IntegrationTests;

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public OrderStatus? OptionalStatus { get; set; }
    public Currency Currency { get; set; } = Currency.Usd;
    public ShipmentState ShipmentState { get; set; } = ShipmentState.InTransit;
}

// Entity for relational key/index tests
public class RelationalOrder
{
    public OrderStatus Id { get; set; } = OrderStatus.Pending;  // PK as enum
    public OrderStatus AlternateKey { get; set; } = OrderStatus.Pending;
    public OrderStatus IndexedStatus { get; set; } = OrderStatus.Pending;
    public int FkId { get; set; }
}

// ---------------------------------------------------------------------------
// Manually-defined converters (mirror what the EFCore generator produces).
// The generator snapshot tests verify the generated templates; these converters
// exercise the same EF Core mechanics without requiring the generator to run
// as an analyzer in this project.
//
// Use dictionary indexer (not TryGetValue/throw) so the lambdas remain valid
// as expression trees, which EF Core requires for some provider scenarios.
// ---------------------------------------------------------------------------

internal sealed class OrderStatusByValueConverter : ValueConverter<OrderStatus, int>
{
    private static readonly Dictionary<int, OrderStatus> s_map = new()
    {
        [OrderStatus.Pending.Value] = OrderStatus.Pending,
        [OrderStatus.Paid.Value]    = OrderStatus.Paid,
        [OrderStatus.Shipped.Value] = OrderStatus.Shipped,
    };

    public OrderStatusByValueConverter() : base(e => e.Value, v => s_map[v]) { }
}

internal sealed class OrderStatusByNameConverter : ValueConverter<OrderStatus, string>
{
    private static readonly Dictionary<string, OrderStatus> s_map = new()
    {
        [OrderStatus.Pending.Name] = OrderStatus.Pending,
        [OrderStatus.Paid.Name]    = OrderStatus.Paid,
        [OrderStatus.Shipped.Name] = OrderStatus.Shipped,
    };

    public OrderStatusByNameConverter() : base(e => e.Name, n => s_map[n]) { }
}

internal sealed class CurrencyByNameConverter : ValueConverter<Currency, string>
{
    private static readonly Dictionary<string, Currency> s_map = new()
    {
        [Currency.Usd.Name] = Currency.Usd,
        [Currency.Eur.Name] = Currency.Eur,
        [Currency.Gbp.Name] = Currency.Gbp,
    };

    public CurrencyByNameConverter() : base(e => e.Name, n => s_map[n]) { }
}

internal sealed class ShipmentStateByValueConverter : ValueConverter<ShipmentState, int>
{
    private static readonly Dictionary<int, ShipmentState> s_map = new()
    {
        [ShipmentState.InTransit.Value] = ShipmentState.InTransit,
        [ShipmentState.Delivered.Value] = ShipmentState.Delivered,
        [ShipmentState.Returned.Value]  = ShipmentState.Returned,
    };

    public ShipmentStateByValueConverter() : base(e => e.Value, v => s_map[v]) { }
}

// ---------------------------------------------------------------------------

public class TestDbContext : DbContext
{
    private readonly Action<ModelBuilder>? _modelConfig;

    public DbSet<Order> Orders { get; set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options, Action<ModelBuilder>? modelConfig = null)
        : base(options)
    {
        _modelConfig = modelConfig;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Manually register converters for each OptimizedEnum type used in this project.
        // This replicates what the generated ConfigureOptimizedEnums() extension does.
        configurationBuilder.Properties<OrderStatus>().HaveConversion<OrderStatusByValueConverter>();
        configurationBuilder.Properties<Currency>().HaveConversion<CurrencyByNameConverter>();
        configurationBuilder.Properties<ShipmentState>().HaveConversion<ShipmentStateByValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _modelConfig?.Invoke(modelBuilder);
    }
}

// Separate context for relational tests (PK/FK/index/alternate key)
public class RelationalTestDbContext : DbContext
{
    public DbSet<RelationalOrder> RelationalOrders { get; set; } = null!;

    public RelationalTestDbContext(DbContextOptions<RelationalTestDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RelationalOrder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => x.AlternateKey);
            entity.HasIndex(x => x.IndexedStatus);
            // Explicitly wire converter on each column (mirrors HasOrderStatusConversionByValue())
            entity.Property(x => x.Id).HasConversion(new OrderStatusByValueConverter());
            entity.Property(x => x.AlternateKey).HasConversion(new OrderStatusByValueConverter());
            entity.Property(x => x.IndexedStatus).HasConversion(new OrderStatusByValueConverter());
        });
    }
}
