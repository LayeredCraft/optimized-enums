using LayeredCraft.OptimizedEnums;

namespace LayeredCraft.OptimizedEnums.EFCore.Tests.IntegrationTests;

// Integer-valued enum — stored by value
public sealed partial class OrderStatus : OptimizedEnum<OrderStatus, int>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Paid    = new(2, nameof(Paid));
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped));

    private OrderStatus(int value, string name) : base(value, name) { }
}

// String-valued enum — stored by name
public sealed partial class Currency : OptimizedEnum<Currency, string>
{
    public static readonly Currency Usd = new("USD", nameof(Usd));
    public static readonly Currency Eur = new("EUR", nameof(Eur));
    public static readonly Currency Gbp = new("GBP", nameof(Gbp));

    private Currency(string value, string name) : base(value, name) { }
}

// Enum through abstract intermediate base
public abstract class ShipmentStateBase<TEnum> : OptimizedEnum<TEnum, int>
    where TEnum : OptimizedEnum<TEnum, int>
{
    protected ShipmentStateBase(int value, string name) : base(value, name) { }
}

public sealed partial class ShipmentState : ShipmentStateBase<ShipmentState>
{
    public static readonly ShipmentState InTransit  = new(1, nameof(InTransit));
    public static readonly ShipmentState Delivered  = new(2, nameof(Delivered));
    public static readonly ShipmentState Returned   = new(3, nameof(Returned));

    private ShipmentState(int value, string name) : base(value, name) { }
}
