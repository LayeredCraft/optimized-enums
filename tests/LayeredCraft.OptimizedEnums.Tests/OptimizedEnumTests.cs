namespace LayeredCraft.OptimizedEnums.Tests;

public class OptimizedEnumTests
{
    [Fact]
    public void All_ReturnsAllThreeMembers()
    {
        OrderStatus.All.Should().HaveCount(3);
        OrderStatus.All.Should().ContainInOrder(OrderStatus.Pending, OrderStatus.Paid, OrderStatus.Shipped);
    }

    [Fact]
    public void Names_ReturnsAllMemberNames()
    {
        OrderStatus.Names.Should().HaveCount(3);
        OrderStatus.Names.Should().ContainInOrder("Pending", "Paid", "Shipped");
    }

    [Fact]
    public void Values_ReturnsAllMemberValues()
    {
        OrderStatus.Values.Should().HaveCount(3);
        OrderStatus.Values.Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void Count_ReturnsThree()
    {
        OrderStatus.Count.Should().Be(3);
    }

    [Fact]
    public void FromName_ValidName_ReturnsCorrectMember()
    {
        OrderStatus.FromName("Pending").Should().Be(OrderStatus.Pending);
        OrderStatus.FromName("Paid").Should().Be(OrderStatus.Paid);
        OrderStatus.FromName("Shipped").Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void FromName_InvalidName_ThrowsKeyNotFoundException()
    {
        var act = () => OrderStatus.FromName("Unknown");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryFromName_ValidName_ReturnsTrueAndSetsResult()
    {
        var found = OrderStatus.TryFromName("Paid", out var result);
        found.Should().BeTrue();
        result.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void TryFromName_InvalidName_ReturnsFalse()
    {
        var found = OrderStatus.TryFromName("Unknown", out var result);
        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void FromValue_ValidValue_ReturnsCorrectMember()
    {
        OrderStatus.FromValue(1).Should().Be(OrderStatus.Pending);
        OrderStatus.FromValue(2).Should().Be(OrderStatus.Paid);
        OrderStatus.FromValue(3).Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void FromValue_InvalidValue_ThrowsKeyNotFoundException()
    {
        var act = () => OrderStatus.FromValue(99);
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryFromValue_ValidValue_ReturnsTrueAndSetsResult()
    {
        var found = OrderStatus.TryFromValue(3, out var result);
        found.Should().BeTrue();
        result.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void TryFromValue_InvalidValue_ReturnsFalse()
    {
        var found = OrderStatus.TryFromValue(99, out var result);
        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void ContainsName_KnownName_ReturnsTrue()
    {
        OrderStatus.ContainsName("Pending").Should().BeTrue();
    }

    [Fact]
    public void ContainsName_UnknownName_ReturnsFalse()
    {
        OrderStatus.ContainsName("Unknown").Should().BeFalse();
    }

    [Fact]
    public void ContainsValue_KnownValue_ReturnsTrue()
    {
        OrderStatus.ContainsValue(1).Should().BeTrue();
    }

    [Fact]
    public void ContainsValue_UnknownValue_ReturnsFalse()
    {
        OrderStatus.ContainsValue(99).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameMember_ReturnsTrue()
    {
        var a = OrderStatus.Pending;
        var b = OrderStatus.Pending;
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentMembers_ReturnsFalse()
    {
        OrderStatus.Pending.Equals(OrderStatus.Paid).Should().BeFalse();
        (OrderStatus.Pending == OrderStatus.Paid).Should().BeFalse();
        (OrderStatus.Pending != OrderStatus.Paid).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        OrderStatus.Pending.ToString().Should().Be("Pending");
        OrderStatus.Paid.ToString().Should().Be("Paid");
    }

    [Fact]
    public void CompareTo_OrdersByValue()
    {
        OrderStatus.Pending.CompareTo(OrderStatus.Paid).Should().BeNegative();
        OrderStatus.Shipped.CompareTo(OrderStatus.Pending).Should().BePositive();
        OrderStatus.Paid.CompareTo(OrderStatus.Paid).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        OrderStatus.Pending.CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void GetHashCode_SameMember_ReturnsSameHash()
    {
        OrderStatus.Pending.GetHashCode().Should().Be(OrderStatus.Pending.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentMembers_ReturnsDifferentHash()
    {
        OrderStatus.Pending.GetHashCode().Should().NotBe(OrderStatus.Paid.GetHashCode());
    }
}
