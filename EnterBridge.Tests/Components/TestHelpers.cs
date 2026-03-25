using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EnterBridge.Data;
using EnterBridge.Models;
using EnterBridge.Services;
using RichardSzalay.MockHttp;

namespace EnterBridge.Tests.Components;

public static class TestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, JsonOptions);

    public static MockHttpMessageHandler SetupMockApi(TestContext ctx)
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.test.com/");
        ctx.Services.AddSingleton(new EnterBridgeApiClient(httpClient));
        return handler;
    }

    public static AppDbContext SetupInMemoryDb(TestContext ctx)
    {
        var dbName = Guid.NewGuid().ToString();
        ctx.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var sp = ctx.Services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        return db;
    }

    public static void SetupCart(TestContext ctx, CartService? cart = null)
    {
        ctx.Services.AddSingleton(cart ?? new CartService());
    }

    public static void MockStatsAndProducts(MockHttpMessageHandler handler, List<Product>? products = null, int totalCount = 0)
    {
        var stats = new ApiStats
        {
            TotalProducts = 407,
            ProductsByCategory = new Dictionary<string, int>
            {
                ["Concrete"] = 45, ["Electrical"] = 44, ["Lumber"] = 46, ["Tools"] = 31
            },
            TotalPrices = 69597
        };
        handler.When("/api/stats").Respond("application/json", Serialize(stats));

        products ??=
        [
            new Product { Id = 1, Name = "Premium Pine 2x4", Sku = "LUM-PP-2408", Category = "Lumber", Description = "Framing lumber" },
            new Product { Id = 2, Name = "Claw Hammer", Sku = "HW-HAM-001", Category = "Tools", Description = "16oz claw hammer" },
        ];

        var pagedProducts = new PagedResult<Product>
        {
            Items = products,
            TotalCount = totalCount > 0 ? totalCount : products.Count,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false
        };
        handler.When("/api/products*").Respond("application/json", Serialize(pagedProducts));
    }

    public static void MockPricesForProduct(MockHttpMessageHandler handler, int productId, decimal latestPrice = 10m)
    {
        var priceResponse = new PagedResult<PriceRecord>
        {
            Items =
            [
                new PriceRecord { Id = 1, Amount = latestPrice, DateTime = DateTime.UtcNow, Quantity = 1, UnitOfMeasure = "Each", ProductId = productId },
                new PriceRecord { Id = 2, Amount = latestPrice - 1m, DateTime = DateTime.UtcNow.AddDays(-7), Quantity = 1, UnitOfMeasure = "Each", ProductId = productId },
                new PriceRecord { Id = 3, Amount = latestPrice - 2m, DateTime = DateTime.UtcNow.AddDays(-14), Quantity = 1, UnitOfMeasure = "Each", ProductId = productId },
            ],
            TotalCount = 3, PageNumber = 1, PageSize = 200, TotalPages = 1, HasNextPage = false
        };
        handler.When("/api/prices*").Respond("application/json", Serialize(priceResponse));
    }

    public static Product TestProduct(int id = 1) => new()
    {
        Id = id,
        Name = "Premium Pine 2x4",
        Description = "Standard framing lumber",
        Sku = "LUM-PP-2408",
        Category = "Lumber"
    };
}
