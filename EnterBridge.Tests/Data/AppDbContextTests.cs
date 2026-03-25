using Microsoft.EntityFrameworkCore;
using EnterBridge.Data;
using EnterBridge.Models;

namespace EnterBridge.Tests.Data;

public class AppDbContextTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task CanCreateAndRetrieveOrder()
    {
        using var db = CreateDb();
        var order = new Order
        {
            SubmittedBy = "Alice",
            Notes = "Rush order",
            Items =
            [
                new OrderItem
                {
                    ProductId = 1, ProductName = "Pine 2x4", ProductSku = "LUM-001",
                    Category = "Lumber", UnitPrice = 10m, UnitOfMeasure = "BoardFeet", Quantity = 5
                }
            ]
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var retrieved = await db.Orders.Include(o => o.Items).FirstAsync(o => o.SubmittedBy == "Alice");
        Assert.Equal("Alice", retrieved.SubmittedBy);
        Assert.Equal("Rush order", retrieved.Notes);
        Assert.Single(retrieved.Items);
        Assert.Equal("Pine 2x4", retrieved.Items[0].ProductName);
        Assert.Equal(50m, retrieved.Total);
    }

    [Fact]
    public async Task CascadeDelete_RemovesOrderItems()
    {
        using var db = CreateDb();
        var order = new Order
        {
            SubmittedBy = "Bob",
            Items = [new OrderItem { ProductId = 1, ProductName = "Hammer", ProductSku = "HW-001", Category = "Hardware", UnitPrice = 15m, UnitOfMeasure = "Each", Quantity = 1 }]
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        db.Orders.Remove(order);
        await db.SaveChangesAsync();

        Assert.Empty(await db.Orders.ToListAsync());
        Assert.Empty(await db.OrderItems.ToListAsync());
    }

    [Fact]
    public async Task StatusStoredAsString()
    {
        using var db = CreateDb();
        var order = new Order { SubmittedBy = "Carol", Status = OrderStatus.Approved };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var retrieved = await db.Orders.FirstAsync();
        Assert.Equal(OrderStatus.Approved, retrieved.Status);
    }

    [Fact]
    public async Task CanUpdateOrderStatus()
    {
        using var db = CreateDb();
        var order = new Order { SubmittedBy = "Dave" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.Status = OrderStatus.Rejected;
        order.ReviewedBy = "Foreman Jim";
        await db.SaveChangesAsync();

        var retrieved = await db.Orders.FirstAsync();
        Assert.Equal(OrderStatus.Rejected, retrieved.Status);
        Assert.Equal("Foreman Jim", retrieved.ReviewedBy);
    }

    [Fact]
    public async Task CanRejectIndividualItems()
    {
        using var db = CreateDb();
        var order = new Order
        {
            SubmittedBy = "Eve",
            Items =
            [
                new OrderItem { ProductId = 1, ProductName = "Item A", ProductSku = "A", Category = "Cat", UnitPrice = 10m, UnitOfMeasure = "Each", Quantity = 1 },
                new OrderItem { ProductId = 2, ProductName = "Item B", ProductSku = "B", Category = "Cat", UnitPrice = 20m, UnitOfMeasure = "Each", Quantity = 1 },
            ]
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var items = await db.OrderItems.ToListAsync();
        items[0].IsRejected = true;
        items[0].RejectionReason = "Wrong size";
        await db.SaveChangesAsync();

        var retrieved = await db.Orders.Include(o => o.Items).FirstAsync();
        Assert.True(retrieved.Items[0].IsRejected);
        Assert.Equal("Wrong size", retrieved.Items[0].RejectionReason);
        Assert.Equal(20m, retrieved.Total); // Only non-rejected item
    }

    [Fact]
    public async Task MultipleOrders_QueryByStatus()
    {
        using var db = CreateDb();
        db.Orders.Add(new Order { SubmittedBy = "A", Status = OrderStatus.Submitted });
        db.Orders.Add(new Order { SubmittedBy = "B", Status = OrderStatus.Approved });
        db.Orders.Add(new Order { SubmittedBy = "C", Status = OrderStatus.Submitted });
        await db.SaveChangesAsync();

        var submitted = await db.Orders.Where(o => o.Status == OrderStatus.Submitted).ToListAsync();
        Assert.Equal(2, submitted.Count);
    }
}
