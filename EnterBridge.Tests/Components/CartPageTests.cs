using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EnterBridge.Components.Pages.Orders;
using EnterBridge.Data;
using EnterBridge.Models;
using EnterBridge.Services;
using Microsoft.EntityFrameworkCore;

namespace EnterBridge.Tests.Components;

public class CartPageTests : TestContext
{
    [Fact]
    public void EmptyCart_ShowsEmptyMessage()
    {
        TestHelpers.SetupCart(this);
        TestHelpers.SetupInMemoryDb(this);

        var cut = RenderComponent<Cart>();

        Assert.Contains("Your cart is empty", cut.Markup);
        Assert.Contains("Browse products", cut.Markup);
    }

    [Fact]
    public void CartWithItems_ShowsTable()
    {
        var cart = new CartService();
        cart.AddItem(
            new Product { Id = 1, Name = "Pine Board", Sku = "LUM-001", Category = "Lumber" },
            new PriceRecord { Amount = 12.50m, UnitOfMeasure = "BoardFeet" },
            3
        );
        TestHelpers.SetupCart(this, cart);
        TestHelpers.SetupInMemoryDb(this);

        var cut = RenderComponent<Cart>();

        Assert.Contains("Pine Board", cut.Markup);
        Assert.Contains("$12.50", cut.Markup);
        Assert.Contains("$37.50", cut.Markup); // Line total
        Assert.Contains("Lumber", cut.Markup);
    }

    [Fact]
    public void CartWithItems_ShowsTotal()
    {
        var cart = new CartService();
        cart.AddItem(
            new Product { Id = 1, Name = "Item A", Sku = "A", Category = "Cat" },
            new PriceRecord { Amount = 10m, UnitOfMeasure = "Each" },
            2
        );
        cart.AddItem(
            new Product { Id = 2, Name = "Item B", Sku = "B", Category = "Cat" },
            new PriceRecord { Amount = 5m, UnitOfMeasure = "Each" },
            1
        );
        TestHelpers.SetupCart(this, cart);
        TestHelpers.SetupInMemoryDb(this);

        var cut = RenderComponent<Cart>();

        Assert.Contains("$25.00", cut.Markup); // 20 + 5
    }

    [Fact]
    public void CartWithItems_ShowsSubmitForm()
    {
        var cart = new CartService();
        cart.AddItem(
            new Product { Id = 1, Name = "Item", Sku = "A", Category = "Cat" },
            new PriceRecord { Amount = 10m, UnitOfMeasure = "Each" }
        );
        TestHelpers.SetupCart(this, cart);
        TestHelpers.SetupInMemoryDb(this);

        var cut = RenderComponent<Cart>();

        Assert.Contains("Submit Order", cut.Markup);
        Assert.Contains("Your Name", cut.Markup);
    }

    [Fact]
    public void RemoveButton_RemovesItem()
    {
        var cart = new CartService();
        cart.AddItem(
            new Product { Id = 1, Name = "Item A", Sku = "A", Category = "Cat" },
            new PriceRecord { Amount = 10m, UnitOfMeasure = "Each" }
        );
        TestHelpers.SetupCart(this, cart);
        TestHelpers.SetupInMemoryDb(this);

        var cut = RenderComponent<Cart>();
        Assert.Contains("Item A", cut.Markup);

        var removeBtn = cut.Find("button.btn-outline-danger");
        removeBtn.Click();

        Assert.Contains("Your cart is empty", cut.Markup);
    }

    [Fact]
    public async Task SubmitOrder_CreatesOrderInDatabase()
    {
        var cart = new CartService();
        cart.AddItem(
            new Product { Id = 1, Name = "Test Product", Sku = "TST-001", Category = "Tools" },
            new PriceRecord { Amount = 25m, UnitOfMeasure = "Each" },
            2
        );
        TestHelpers.SetupCart(this, cart);
        var db = TestHelpers.SetupInMemoryDb(this);

        var cut = RenderComponent<Cart>();

        // Fill in the name
        var nameInput = cut.Find("input[placeholder='Enter your name']");
        nameInput.Change("Alice");

        // Submit
        var submitBtn = cut.Find("button.btn-primary");
        submitBtn.Click();

        // Verify order was created
        await Task.Delay(100); // Allow async save
        var orders = await db.Orders.Include(o => o.Items).ToListAsync();
        Assert.Single(orders);
        Assert.Equal("Alice", orders[0].SubmittedBy);
        Assert.Single(orders[0].Items);
        Assert.Equal(25m, orders[0].Items[0].UnitPrice);
        Assert.Equal(2, orders[0].Items[0].Quantity);
    }
}
