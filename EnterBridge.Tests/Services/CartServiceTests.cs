using EnterBridge.Models;
using EnterBridge.Services;

namespace EnterBridge.Tests.Services;

public class CartServiceTests
{
    private static Product MakeProduct(int id = 1, string name = "Test Product") =>
        new() { Id = id, Name = name, Sku = "TST-001", Category = "Tools" };

    private static PriceRecord MakePrice(decimal amount = 10.00m, string uom = "Each") =>
        new() { Id = 1, Amount = amount, UnitOfMeasure = uom, ProductId = 1 };

    [Fact]
    public void NewCart_IsEmpty()
    {
        var cart = new CartService();
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.Count);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public void AddItem_AddsNewProduct()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice(10m));

        Assert.Equal(1, cart.Count);
        Assert.Equal(10m, cart.Total);
        Assert.Equal(1, cart.Items[0].Quantity);
    }

    [Fact]
    public void AddItem_WithQuantity_SetsCorrectQuantity()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice(5m), quantity: 3);

        Assert.Equal(1, cart.Count);
        Assert.Equal(3, cart.Items[0].Quantity);
        Assert.Equal(15m, cart.Total);
    }

    [Fact]
    public void AddItem_SameProductTwice_IncrementsQuantity()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1), MakePrice(10m));
        cart.AddItem(MakeProduct(1), MakePrice(10m), 2);

        Assert.Equal(1, cart.Count);
        Assert.Equal(3, cart.Items[0].Quantity);
        Assert.Equal(30m, cart.Total);
    }

    [Fact]
    public void AddItem_DifferentProducts_AddsSeparately()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1, "Product A"), MakePrice(10m));
        cart.AddItem(MakeProduct(2, "Product B"), MakePrice(20m));

        Assert.Equal(2, cart.Count);
        Assert.Equal(30m, cart.Total);
    }

    [Fact]
    public void RemoveItem_RemovesProduct()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1), MakePrice(10m));
        cart.AddItem(MakeProduct(2), MakePrice(20m));

        cart.RemoveItem(1);

        Assert.Equal(1, cart.Count);
        Assert.Equal(2, cart.Items[0].Product.Id);
    }

    [Fact]
    public void RemoveItem_NonExistentProduct_DoesNothing()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1), MakePrice(10m));

        cart.RemoveItem(999);

        Assert.Equal(1, cart.Count);
    }

    [Fact]
    public void UpdateQuantity_ChangesQuantity()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice(10m));

        cart.UpdateQuantity(1, 5);

        Assert.Equal(5, cart.Items[0].Quantity);
        Assert.Equal(50m, cart.Total);
    }

    [Fact]
    public void UpdateQuantity_ZeroOrNegative_RemovesItem()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice(10m));

        cart.UpdateQuantity(1, 0);

        Assert.Equal(0, cart.Count);
    }

    [Fact]
    public void UpdateQuantity_NegativeValue_RemovesItem()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice(10m));

        cart.UpdateQuantity(1, -1);

        Assert.Equal(0, cart.Count);
    }

    [Fact]
    public void UpdateQuantity_NonExistentProduct_DoesNothing()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1), MakePrice(10m));

        cart.UpdateQuantity(999, 5);

        Assert.Equal(1, cart.Items[0].Quantity);
    }

    [Fact]
    public void Clear_EmptiesCart()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1), MakePrice(10m));
        cart.AddItem(MakeProduct(2), MakePrice(20m));

        cart.Clear();

        Assert.Equal(0, cart.Count);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public void OnChange_FiresOnAdd()
    {
        var cart = new CartService();
        var fired = false;
        cart.OnChange += () => fired = true;

        cart.AddItem(MakeProduct(), MakePrice());

        Assert.True(fired);
    }

    [Fact]
    public void OnChange_FiresOnRemove()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice());
        var fired = false;
        cart.OnChange += () => fired = true;

        cart.RemoveItem(1);

        Assert.True(fired);
    }

    [Fact]
    public void OnChange_FiresOnUpdateQuantity()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice());
        var fired = false;
        cart.OnChange += () => fired = true;

        cart.UpdateQuantity(1, 5);

        Assert.True(fired);
    }

    [Fact]
    public void OnChange_FiresOnClear()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(), MakePrice());
        var fired = false;
        cart.OnChange += () => fired = true;

        cart.Clear();

        Assert.True(fired);
    }

    [Fact]
    public void CartItem_LineTotal_CalculatesCorrectly()
    {
        var item = new CartItem
        {
            Product = MakeProduct(),
            Price = MakePrice(12.50m),
            Quantity = 4
        };

        Assert.Equal(50.00m, item.LineTotal);
    }

    [Fact]
    public void Total_SumsAllLineTotals()
    {
        var cart = new CartService();
        cart.AddItem(MakeProduct(1), MakePrice(10m), 2);   // 20
        cart.AddItem(MakeProduct(2), MakePrice(5.50m), 3); // 16.50

        Assert.Equal(36.50m, cart.Total);
    }
}
