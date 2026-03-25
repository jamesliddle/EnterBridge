using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EnterBridge.Components.Pages.Orders;
using EnterBridge.Data;
using EnterBridge.Models;
using EnterBridge.Services;
using Microsoft.EntityFrameworkCore;
using RichardSzalay.MockHttp;

namespace EnterBridge.Tests.Components;

public class OrderDetailPageTests : TestContext
{
    private AppDbContext SetupAll()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        handler.When("/api/products/*").Respond("application/json", TestHelpers.Serialize(TestHelpers.TestProduct()));
        TestHelpers.MockPricesForProduct(handler, 1, 10m);
        return TestHelpers.SetupInMemoryDb(this);
    }

    private Order SeedOrder(AppDbContext db, OrderStatus status = OrderStatus.Submitted)
    {
        var order = new Order
        {
            SubmittedBy = "Alice",
            Status = status,
            Notes = "Test order",
            Items =
            [
                new OrderItem { ProductId = 1, ProductName = "Pine 2x4", ProductSku = "LUM-001", Category = "Lumber", UnitPrice = 10m, UnitOfMeasure = "BoardFeet", Quantity = 5 },
                new OrderItem { ProductId = 2, ProductName = "Hammer", ProductSku = "HW-001", Category = "Hardware", UnitPrice = 15m, UnitOfMeasure = "Each", Quantity = 1 },
            ]
        };
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    [Fact]
    public void Renders_OrderDetails()
    {
        var db = SetupAll();
        var order = SeedOrder(db);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Alice"));
        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("Pine 2x4", cut.Markup);
        Assert.Contains("Hammer", cut.Markup);
        Assert.Contains("$10.00", cut.Markup);
        Assert.Contains("Test order", cut.Markup);
    }

    [Fact]
    public void Renders_OrderTotal()
    {
        var db = SetupAll();
        var order = SeedOrder(db);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("$65.00")); // 50 + 15
        Assert.Contains("$65.00", cut.Markup);
    }

    [Fact]
    public void SubmittedOrder_ShowsReviewControls()
    {
        var db = SetupAll();
        var order = SeedOrder(db, OrderStatus.Submitted);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Foreman Review"));
        Assert.Contains("Approve Order", cut.Markup);
        Assert.Contains("Reject Entire Order", cut.Markup);
        Assert.Contains("Reviewer Name", cut.Markup);
    }

    [Fact]
    public void ApprovedOrder_HidesReviewControls()
    {
        var db = SetupAll();
        var order = SeedOrder(db, OrderStatus.Approved);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Alice"));
        Assert.DoesNotContain("Foreman Review", cut.Markup);
        Assert.DoesNotContain("Approve Order", cut.Markup);
    }

    [Fact]
    public void Renders_StatusBadge()
    {
        var db = SetupAll();
        var order = SeedOrder(db, OrderStatus.Submitted);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Submitted"));
        var badge = cut.Find(".badge");
        Assert.Contains("Submitted", badge.TextContent);
    }

    [Fact]
    public void SubmittedOrder_ShowsRejectButtons()
    {
        var db = SetupAll();
        var order = SeedOrder(db, OrderStatus.Submitted);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Reject"));
        var rejectBtns = cut.FindAll("button").Where(b => b.TextContent.Contains("Reject") && !b.TextContent.Contains("Entire")).ToList();
        Assert.Equal(2, rejectBtns.Count); // One per item
    }

    [Fact]
    public void Renders_ReorderButton()
    {
        var db = SetupAll();
        var order = SeedOrder(db);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Reorder"));
        Assert.Contains("Reorder These Items", cut.Markup);
    }

    [Fact]
    public void Renders_Breadcrumb()
    {
        var db = SetupAll();
        var order = SeedOrder(db);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("breadcrumb"));
        Assert.Contains("Orders", cut.Markup);
        Assert.Contains($"Order #{order.Id}", cut.Markup);
    }

    [Fact]
    public async Task ApproveOrder_UpdatesStatus()
    {
        var db = SetupAll();
        var order = SeedOrder(db, OrderStatus.Submitted);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Foreman Review"));

        // Enter reviewer name
        var nameInput = cut.Find("input[placeholder='Your name']");
        nameInput.Change("Foreman Jim");

        // Wait for re-render with enabled button
        cut.WaitForState(() =>
        {
            var btn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Approve Order"));
            return btn != null && !btn.HasAttribute("disabled");
        });

        var approveBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Approve Order"));
        approveBtn.Click();

        await Task.Delay(200);
        db.ChangeTracker.Clear();
        var updated = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Approved, updated.Status);
        Assert.Equal("Foreman Jim", updated.ReviewedBy);
    }

    [Fact]
    public async Task RejectOrder_UpdatesStatus()
    {
        var db = SetupAll();
        var order = SeedOrder(db, OrderStatus.Submitted);

        var cut = RenderComponent<OrderDetail>(p => p.Add(x => x.OrderId, order.Id));

        cut.WaitForState(() => cut.Markup.Contains("Foreman Review"));

        var nameInput = cut.Find("input[placeholder='Your name']");
        nameInput.Change("Foreman Bob");

        cut.WaitForState(() =>
        {
            var btn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Reject Entire Order"));
            return btn != null && !btn.HasAttribute("disabled");
        });

        var rejectBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Reject Entire Order"));
        rejectBtn.Click();

        await Task.Delay(200);
        db.ChangeTracker.Clear();
        var updated = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Rejected, updated.Status);
        Assert.Equal("Foreman Bob", updated.ReviewedBy);
    }
}
