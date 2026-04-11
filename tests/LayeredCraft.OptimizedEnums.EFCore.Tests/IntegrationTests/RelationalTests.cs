using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LayeredCraft.OptimizedEnums.EFCore.Tests.IntegrationTests;

/// <summary>
/// Relational model tests (PK, FK, alternate key, index) using PostgreSQL via Testcontainers.
/// </summary>
public class RelationalTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private RelationalTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RelationalTestDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new RelationalTestDbContext(options);
    }

    [Fact]
    public async Task PrimaryKey_AsEnum_WorksCorrectly()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();

        ctx.RelationalOrders.Add(new RelationalOrder
        {
            Id = OrderStatus.Pending,
            AlternateKey = OrderStatus.Paid,
            IndexedStatus = OrderStatus.Shipped,
        });
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.RelationalOrders.FindAsync(OrderStatus.Pending);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task AlternateKey_AsEnum_WorksCorrectly()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();

        ctx.RelationalOrders.Add(new RelationalOrder
        {
            Id = OrderStatus.Pending,
            AlternateKey = OrderStatus.Paid,
            IndexedStatus = OrderStatus.Shipped,
        });
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.RelationalOrders
            .FirstOrDefaultAsync(x => x.AlternateKey == OrderStatus.Paid);
        loaded.Should().NotBeNull();
        loaded!.AlternateKey.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task Index_AsEnum_WorksCorrectly()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();

        ctx.RelationalOrders.Add(new RelationalOrder
        {
            Id = OrderStatus.Pending,
            AlternateKey = OrderStatus.Paid,
            IndexedStatus = OrderStatus.Shipped,
        });
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();

        var loaded = await ctx.RelationalOrders
            .Where(x => x.IndexedStatus == OrderStatus.Shipped)
            .ToListAsync();
        loaded.Should().HaveCount(1);
    }

    [Fact]
    public async Task Schema_IsCreated_WithoutErrors()
    {
        await using var ctx = CreateContext();

        // EnsureCreated should succeed — enum columns get proper column types
        var created = await ctx.Database.EnsureCreatedAsync();
        created.Should().BeTrue();
    }
}
