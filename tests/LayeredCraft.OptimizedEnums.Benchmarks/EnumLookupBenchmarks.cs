using BenchmarkDotNet.Attributes;

namespace LayeredCraft.OptimizedEnums.Benchmarks;

public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));
    public static readonly OrderStatus Delivered = new(4, nameof(Delivered));
    public static readonly OrderStatus Cancelled = new(5, nameof(Cancelled));

    private OrderStatus(int value, string name) : base(value, name) { }
}

[MemoryDiagnoser]
public class EnumLookupBenchmarks
{
    [Benchmark]
    public OrderStatus FromName() => OrderStatus.FromName("Shipped");

    [Benchmark]
    public bool TryFromName()
    {
        OrderStatus.TryFromName("Shipped", out var result);
        return result is not null;
    }

    [Benchmark]
    public OrderStatus FromValue() => OrderStatus.FromValue(3);

    [Benchmark]
    public bool TryFromValue()
    {
        OrderStatus.TryFromValue(3, out var result);
        return result is not null;
    }

    [Benchmark]
    public bool ContainsName() => OrderStatus.ContainsName("Shipped");

    [Benchmark]
    public bool ContainsValue() => OrderStatus.ContainsValue(3);

    [Benchmark]
    public int GetAll() => OrderStatus.All.Count;

    [Benchmark]
    public int GetCount() => OrderStatus.Count;
}
