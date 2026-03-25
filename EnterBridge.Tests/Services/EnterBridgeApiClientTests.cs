using System.Net;
using System.Text.Json;
using EnterBridge.Models;
using EnterBridge.Services;
using RichardSzalay.MockHttp;

namespace EnterBridge.Tests.Services;

public class EnterBridgeApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static (EnterBridgeApiClient client, MockHttpMessageHandler handler) CreateClient()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.test.com/");
        return (new EnterBridgeApiClient(httpClient), handler);
    }

    private static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, JsonOptions);

    [Fact]
    public async Task GetProductsAsync_DefaultParams_ReturnsProducts()
    {
        var (client, handler) = CreateClient();
        var response = new PagedResult<Product> { Items = [new Product { Id = 1, Name = "Pine" }], TotalCount = 1, PageNumber = 1, PageSize = 20, TotalPages = 1 };
        handler.When("/api/products*").Respond("application/json", Serialize(response));

        var result = await client.GetProductsAsync();

        Assert.Single(result.Items);
        Assert.Equal("Pine", result.Items[0].Name);
    }

    [Fact]
    public async Task GetProductsAsync_WithCategory_IncludesCategoryInUrl()
    {
        var (client, handler) = CreateClient();
        string? capturedUrl = null;
        var response = new PagedResult<Product> { Items = [], TotalCount = 0 };
        handler.When("/api/products*").Respond(_ =>
        {
            capturedUrl = _.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        });

        await client.GetProductsAsync(category: "Lumber");

        Assert.NotNull(capturedUrl);
        Assert.Contains("category=Lumber", capturedUrl);
    }

    [Fact]
    public async Task GetProductsAsync_WithSearch_IncludesNameInUrl()
    {
        var (client, handler) = CreateClient();
        string? capturedUrl = null;
        var response = new PagedResult<Product> { Items = [], TotalCount = 0 };
        handler.When("/api/products*").Respond(_ =>
        {
            capturedUrl = _.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        });

        await client.GetProductsAsync(search: "pine");

        Assert.NotNull(capturedUrl);
        Assert.Contains("name=pine", capturedUrl);
    }

    [Fact]
    public async Task GetProductsAsync_WithPagination_IncludesPageParams()
    {
        var (client, handler) = CreateClient();
        string? capturedUrl = null;
        var response = new PagedResult<Product> { Items = [], PageNumber = 3, PageSize = 10, TotalCount = 50, TotalPages = 5 };
        handler.When("/api/products*").Respond(_ =>
        {
            capturedUrl = _.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        });

        var result = await client.GetProductsAsync(page: 3, pageSize: 10);

        Assert.NotNull(capturedUrl);
        Assert.Contains("pageNumber=3", capturedUrl);
        Assert.Contains("pageSize=10", capturedUrl);
        Assert.Equal(3, result.PageNumber);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsProduct()
    {
        var (client, handler) = CreateClient();
        var product = new Product { Id = 42, Name = "Hammer", Sku = "HW-HAM-001", Category = "Hardware" };
        handler.When("/api/products/42").Respond("application/json", Serialize(product));

        var result = await client.GetProductAsync(42);

        Assert.NotNull(result);
        Assert.Equal("Hammer", result.Name);
        Assert.Equal("Hardware", result.Category);
    }

    [Fact]
    public async Task GetPricesAsync_ReturnsResults()
    {
        var (client, handler) = CreateClient();
        var response = new PagedResult<PriceRecord>
        {
            Items = [new PriceRecord { Id = 1, Amount = 10.50m, ProductId = 1 }],
            TotalCount = 1, PageNumber = 1, PageSize = 200, TotalPages = 1
        };
        handler.When("/api/prices*").Respond("application/json", Serialize(response));

        var result = await client.GetPricesAsync(1);

        Assert.Single(result.Items);
        Assert.Equal(10.50m, result.Items[0].Amount);
    }

    [Fact]
    public async Task GetPricesAsync_WithDateRange_IncludesDateParams()
    {
        var (client, handler) = CreateClient();
        string? capturedUrl = null;
        var response = new PagedResult<PriceRecord> { Items = [], TotalCount = 0 };
        handler.When("/api/prices*").Respond(_ =>
        {
            capturedUrl = _.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        });

        await client.GetPricesAsync(1, startDate: "2024-01-01", endDate: "2024-06-01");

        Assert.NotNull(capturedUrl);
        Assert.Contains("startDate=2024-01-01", capturedUrl);
        Assert.Contains("endDate=2024-06-01", capturedUrl);
    }

    [Fact]
    public async Task GetAllPricesAsync_PaginatesThroughAllPages()
    {
        var (client, handler) = CreateClient();
        int callCount = 0;

        handler.When("/api/prices*").Respond(_ =>
        {
            callCount++;
            PagedResult<PriceRecord> page = callCount == 1
                ? new() { Items = [new PriceRecord { Id = 1, Amount = 10m }], PageNumber = 1, TotalPages = 2, HasNextPage = true, TotalCount = 2 }
                : new() { Items = [new PriceRecord { Id = 2, Amount = 20m }], PageNumber = 2, TotalPages = 2, HasNextPage = false, TotalCount = 2 };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(page), System.Text.Encoding.UTF8, "application/json")
            };
        });

        var result = await client.GetAllPricesAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal(10m, result[0].Amount);
        Assert.Equal(20m, result[1].Amount);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetLatestPriceAsync_ReturnsMostRecentPrice()
    {
        var (client, handler) = CreateClient();
        var response = new PagedResult<PriceRecord>
        {
            Items = [new PriceRecord { Id = 1, Amount = 45.07m, ProductId = 1 }],
            TotalCount = 65, PageNumber = 1, PageSize = 1, TotalPages = 65
        };
        handler.When("/api/prices*").Respond("application/json", Serialize(response));

        var result = await client.GetLatestPriceAsync(1);

        Assert.NotNull(result);
        Assert.Equal(45.07m, result.Amount);
    }

    [Fact]
    public async Task GetLatestPriceAsync_NoPrices_ReturnsNull()
    {
        var (client, handler) = CreateClient();
        var response = new PagedResult<PriceRecord> { Items = [], TotalCount = 0 };
        handler.When("/api/prices*").Respond("application/json", Serialize(response));

        var result = await client.GetLatestPriceAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStats()
    {
        var (client, handler) = CreateClient();
        var stats = new ApiStats
        {
            TotalProducts = 407,
            ProductsByCategory = new Dictionary<string, int> { ["Lumber"] = 46, ["Tools"] = 31 },
            TotalPrices = 69597
        };
        handler.When("/api/stats").Respond("application/json", Serialize(stats));

        var result = await client.GetStatsAsync();

        Assert.NotNull(result);
        Assert.Equal(407, result.TotalProducts);
        Assert.Equal(2, result.ProductsByCategory.Count);
        Assert.Equal(69597, result.TotalPrices);
    }

    [Fact]
    public async Task GetProductsAsync_NoCategoryNoSearch_OmitsThoseParams()
    {
        var (client, handler) = CreateClient();
        string? capturedUrl = null;
        var response = new PagedResult<Product> { Items = [], TotalCount = 0 };
        handler.When("/api/products*").Respond(_ =>
        {
            capturedUrl = _.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        });

        await client.GetProductsAsync();

        Assert.NotNull(capturedUrl);
        Assert.DoesNotContain("category=", capturedUrl);
        Assert.DoesNotContain("name=", capturedUrl);
    }

    [Fact]
    public async Task GetAllPricesAsync_SinglePage_ReturnsWithoutExtraRequests()
    {
        var (client, handler) = CreateClient();
        int callCount = 0;
        var response = new PagedResult<PriceRecord>
        {
            Items = [new PriceRecord { Id = 1, Amount = 5m }],
            PageNumber = 1, TotalPages = 1, HasNextPage = false, TotalCount = 1
        };
        handler.When("/api/prices*").Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        });

        var result = await client.GetAllPricesAsync(1);

        Assert.Single(result);
        Assert.Equal(1, callCount);
    }
}
