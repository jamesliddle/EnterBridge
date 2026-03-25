using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EnterBridge.Components.Pages.Products;
using EnterBridge.Models;
using EnterBridge.Services;
using RichardSzalay.MockHttp;

namespace EnterBridge.Tests.Components;

public class ProductDetailPageTests : TestContext
{
    private MockHttpMessageHandler SetupAll()
    {
        var handler = TestHelpers.SetupMockApi(this);
        TestHelpers.SetupCart(this);
        return handler;
    }

    private void MockProductAndPrices(MockHttpMessageHandler handler, int productId = 1)
    {
        var product = new Product { Id = productId, Name = "Premium Pine 2x4", Sku = "LUM-PP-2408", Category = "Lumber", Description = "Standard framing lumber" };
        handler.When($"/api/products/{productId}").Respond("application/json", TestHelpers.Serialize(product));

        var prices = Enumerable.Range(0, 52).Select(i => new PriceRecord
        {
            Id = i + 1,
            Amount = 40m + (i % 10),
            DateTime = DateTime.UtcNow.AddDays(-7 * i),
            Quantity = 1,
            UnitOfMeasure = "BoardFeet",
            ProductId = productId
        }).ToList();

        var priceResponse = new PagedResult<PriceRecord>
        {
            Items = prices,
            TotalCount = prices.Count,
            PageNumber = 1,
            PageSize = 200,
            TotalPages = 1,
            HasNextPage = false
        };
        handler.When("/api/prices*").Respond("application/json", TestHelpers.Serialize(priceResponse));
    }

    [Fact]
    public void Renders_ProductInfo()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Premium Pine"));
        Assert.Contains("Premium Pine 2x4", cut.Markup);
        Assert.Contains("Lumber", cut.Markup);
        Assert.Contains("LUM-PP-2408", cut.Markup);
        Assert.Contains("Standard framing lumber", cut.Markup);
    }

    [Fact]
    public void Renders_CurrentPrice()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Current Price"));
        Assert.Contains("Current Price", cut.Markup);
        Assert.Contains("BoardFeet", cut.Markup);
    }

    [Fact]
    public void Renders_PriceHistory()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Price History"));
        Assert.Contains("Price History", cut.Markup);
    }

    [Fact]
    public void Renders_TimeRangeButtons()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Price History"));
        var buttons = cut.FindAll(".btn-group button");
        var labels = buttons.Select(b => b.TextContent).ToList();
        Assert.Contains("3M", labels);
        Assert.Contains("6M", labels);
        Assert.Contains("1Y", labels);
        Assert.Contains("All", labels);
    }

    [Fact]
    public void DefaultsToAll_TimeRange()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Price History"));
        var allBtn = cut.FindAll(".btn-group button").First(b => b.TextContent == "All");
        Assert.Contains("btn-primary", allBtn.ClassName);
    }

    [Fact]
    public void Renders_PriceTable()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.FindAll("table").Count > 0);
        Assert.Contains("Date", cut.Markup);
        Assert.Contains("Price", cut.Markup);
        Assert.Contains("Change", cut.Markup);
    }

    [Fact]
    public void Renders_PriceChart()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.FindAll(".price-bar").Count > 0);
        Assert.True(cut.FindAll(".price-bar").Count > 0);
    }

    [Fact]
    public void Renders_AddToCartButton()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Add to Cart"));
        Assert.Contains("Add to Cart", cut.Markup);
    }

    [Fact]
    public void Renders_QuantityInput()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.FindAll("input[type='number']").Count > 0);
        var qtyInput = cut.Find("input[type='number']");
        Assert.NotNull(qtyInput);
    }

    [Fact]
    public void Renders_Breadcrumb()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("breadcrumb"));
        Assert.Contains("Products", cut.Markup);
    }

    [Fact]
    public void Renders_PriceChangeIndicator()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("3 months ago"));
        Assert.Contains("3 months ago", cut.Markup);
    }

    [Fact]
    public void Renders_HighLow()
    {
        var handler = SetupAll();
        MockProductAndPrices(handler);

        var cut = RenderComponent<ProductDetail>(p => p.Add(x => x.ProductId, 1));

        cut.WaitForState(() => cut.Markup.Contains("Low:"));
        Assert.Contains("Low:", cut.Markup);
        Assert.Contains("High:", cut.Markup);
    }
}
