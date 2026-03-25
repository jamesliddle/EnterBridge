using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EnterBridge.Components.Pages.Orders;
using EnterBridge.Data;
using EnterBridge.Models;
using EnterBridge.Services;
using Microsoft.EntityFrameworkCore;
using RichardSzalay.MockHttp;

namespace EnterBridge.Tests.Components;

public class OrderListPageTests : TestContext
{
    private AppDbContext SetupAll()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        handler.When("/api/products/*").Respond("application/json", TestHelpers.Serialize(TestHelpers.TestProduct()));
        TestHelpers.MockPricesForProduct(handler, 1, 10m);
        return TestHelpers.SetupInMemoryDb(this);
    }

    [Fact]
    public void NoOrders_ShowsEmptyMessage()
    {
        SetupAll();

        var cut = RenderComponent<OrderList>();

        cut.WaitForState(() => cut.Markup.Contains("No orders found"));
        Assert.Contains("No orders found", cut.Markup);
    }

    [Fact]
    public void WithOrders_ShowsOrderTable()
    {
        var db = SetupAll();
        db.Orders.Add(new Order
        {
            SubmittedBy = "Alice",
            Status = OrderStatus.Submitted,
            Items = [new OrderItem { ProductId = 1, ProductName = "Pine", ProductSku = "L-1", Category = "Lumber", UnitPrice = 10m, UnitOfMeasure = "Each", Quantity = 2 }]
        });
        db.SaveChanges();

        var cut = RenderComponent<OrderList>();

        cut.WaitForState(() => cut.Markup.Contains("Alice"));
        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("$20.00", cut.Markup);
        Assert.Contains("Submitted", cut.Markup);
    }

    [Fact]
    public void WithOrders_ShowsReviewButton()
    {
        var db = SetupAll();
        db.Orders.Add(new Order
        {
            SubmittedBy = "Bob",
            Items = [new OrderItem { ProductId = 1, ProductName = "Hammer", ProductSku = "H-1", Category = "Hardware", UnitPrice = 15m, UnitOfMeasure = "Each", Quantity = 1 }]
        });
        db.SaveChanges();

        var cut = RenderComponent<OrderList>();

        cut.WaitForState(() => cut.Markup.Contains("Review"));
        Assert.Contains("Review", cut.Markup);
    }

    [Fact]
    public void WithOrders_ShowsReorderButton()
    {
        var db = SetupAll();
        db.Orders.Add(new Order
        {
            SubmittedBy = "Carol",
            Items = [new OrderItem { ProductId = 1, ProductName = "Pine", ProductSku = "L-1", Category = "Lumber", UnitPrice = 10m, UnitOfMeasure = "Each", Quantity = 1 }]
        });
        db.SaveChanges();

        var cut = RenderComponent<OrderList>();

        cut.WaitForState(() => cut.Markup.Contains("Reorder"));
        Assert.Contains("Reorder", cut.Markup);
    }

    [Fact]
    public void StatusFilter_ShowsAllStatuses()
    {
        var db = SetupAll();
        db.Orders.Add(new Order { SubmittedBy = "A", Status = OrderStatus.Submitted });
        db.SaveChanges();

        var cut = RenderComponent<OrderList>();

        cut.WaitForState(() => cut.Markup.Contains("All Statuses"));
        var options = cut.FindAll("select option");
        Assert.Contains(options, o => o.TextContent == "All Statuses");
        Assert.Contains(options, o => o.TextContent == "Submitted");
        Assert.Contains(options, o => o.TextContent == "Approved");
        Assert.Contains(options, o => o.TextContent == "Rejected");
    }

    [Fact]
    public void MultipleOrders_SortedByDate()
    {
        var db = SetupAll();
        db.Orders.Add(new Order { SubmittedBy = "First", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        db.Orders.Add(new Order { SubmittedBy = "Second", CreatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var cut = RenderComponent<OrderList>();

        cut.WaitForState(() => cut.Markup.Contains("First"));
        var markup = cut.Markup;
        // "Second" should appear before "First" (newest first)
        Assert.True(markup.IndexOf("Second") < markup.IndexOf("First"));
    }
}
