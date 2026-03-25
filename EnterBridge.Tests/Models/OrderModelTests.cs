using EnterBridge.Models;

namespace EnterBridge.Tests.Models;

public class OrderModelTests
{
    [Fact]
    public void OrderItem_LineTotal_CalculatesCorrectly()
    {
        var item = new OrderItem { UnitPrice = 15.00m, Quantity = 3 };
        Assert.Equal(45.00m, item.LineTotal);
    }

    [Fact]
    public void OrderItem_LineTotal_SingleQuantity()
    {
        var item = new OrderItem { UnitPrice = 7.50m, Quantity = 1 };
        Assert.Equal(7.50m, item.LineTotal);
    }

    [Fact]
    public void Order_Total_SumsNonRejectedItems()
    {
        var order = new Order
        {
            Items =
            [
                new OrderItem { UnitPrice = 10m, Quantity = 2, IsRejected = false },
                new OrderItem { UnitPrice = 5m, Quantity = 3, IsRejected = false },
                new OrderItem { UnitPrice = 100m, Quantity = 1, IsRejected = true },
            ]
        };

        Assert.Equal(35m, order.Total); // 20 + 15, not 135
    }

    [Fact]
    public void Order_Total_AllRejected_ReturnsZero()
    {
        var order = new Order
        {
            Items =
            [
                new OrderItem { UnitPrice = 10m, Quantity = 1, IsRejected = true },
                new OrderItem { UnitPrice = 20m, Quantity = 1, IsRejected = true },
            ]
        };

        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void Order_Total_EmptyItems_ReturnsZero()
    {
        var order = new Order();
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void Order_DefaultStatus_IsSubmitted()
    {
        var order = new Order();
        Assert.Equal(OrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void Order_DefaultItems_IsEmptyList()
    {
        var order = new Order();
        Assert.NotNull(order.Items);
        Assert.Empty(order.Items);
    }

    [Fact]
    public void OrderItem_DefaultQuantity_IsOne()
    {
        var item = new OrderItem();
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public void OrderItem_DefaultIsRejected_IsFalse()
    {
        var item = new OrderItem();
        Assert.False(item.IsRejected);
    }
}
