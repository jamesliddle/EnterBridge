using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EnterBridge.Components.Pages;
using EnterBridge.Models;
using EnterBridge.Services;
using RichardSzalay.MockHttp;

namespace EnterBridge.Tests.Components;

public class HomePageTests : TestContext
{
    [Fact]
    public void Renders_ProductCards()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        TestHelpers.MockStatsAndProducts(handler);
        TestHelpers.MockPricesForProduct(handler, 1, 10m);

        var cut = RenderComponent<Home>();

        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        Assert.Contains("Premium Pine 2x4", cut.Markup);
        Assert.Contains("Claw Hammer", cut.Markup);
    }

    [Fact]
    public void Renders_CategoryFilter()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        TestHelpers.MockStatsAndProducts(handler);

        var cut = RenderComponent<Home>();

        cut.WaitForState(() => cut.FindAll("option").Count > 1);
        var options = cut.FindAll("select option");
        Assert.Contains(options, o => o.TextContent == "Lumber");
        Assert.Contains(options, o => o.TextContent == "Tools");
        Assert.Contains(options, o => o.TextContent == "All Categories");
    }

    [Fact]
    public void Renders_PaginationControls()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);

        var products = Enumerable.Range(1, 20).Select(i => new Product
        {
            Id = i, Name = $"Product {i}", Sku = $"SKU-{i}", Category = "Tools", Description = "Desc"
        }).ToList();

        var stats = new ApiStats
        {
            TotalProducts = 50,
            ProductsByCategory = new Dictionary<string, int> { ["Tools"] = 50 },
            TotalPrices = 1000
        };
        handler.When("/api/stats").Respond("application/json", TestHelpers.Serialize(stats));

        var pagedProducts = new PagedResult<Product>
        {
            Items = products, TotalCount = 50, PageNumber = 1, PageSize = 20, TotalPages = 3, HasNextPage = true, HasPreviousPage = false
        };
        handler.When("/api/products*").Respond("application/json", TestHelpers.Serialize(pagedProducts));

        var cut = RenderComponent<Home>();

        cut.WaitForState(() => cut.FindAll(".page-item").Count > 0);
        var pageItems = cut.FindAll(".page-link");
        Assert.Contains(pageItems, p => p.TextContent == "First");
        Assert.Contains(pageItems, p => p.TextContent == "Next");
        Assert.Contains(pageItems, p => p.TextContent == "Last");
    }

    [Fact]
    public void Renders_PageSizeSelector()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        TestHelpers.MockStatsAndProducts(handler);

        var cut = RenderComponent<Home>();

        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        var selects = cut.FindAll("select");
        // Should have category select + page size select
        Assert.True(selects.Count >= 2);
        Assert.Contains("Per page:", cut.Markup);
    }

    [Fact]
    public void Renders_SearchInput()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        TestHelpers.MockStatsAndProducts(handler);

        var cut = RenderComponent<Home>();

        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        var searchInput = cut.Find("input[placeholder='Search products...']");
        Assert.NotNull(searchInput);
    }

    [Fact]
    public void ShowsLoading_Initially()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        // Don't set up responses so it stays loading
        handler.When("/api/stats").Respond(async () =>
        {
            await Task.Delay(5000);
            return new HttpResponseMessage();
        });

        var cut = RenderComponent<Home>();

        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public void EmptyResults_ShowsMessage()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);

        var stats = new ApiStats { TotalProducts = 0, ProductsByCategory = [], TotalPrices = 0 };
        handler.When("/api/stats").Respond("application/json", TestHelpers.Serialize(stats));
        handler.When("/api/products*").Respond("application/json",
            TestHelpers.Serialize(new PagedResult<Product> { Items = [], TotalCount = 0 }));

        var cut = RenderComponent<Home>();

        cut.WaitForState(() => cut.Markup.Contains("No products found"));
        Assert.Contains("No products found", cut.Markup);
    }
}
