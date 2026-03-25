using EnterBridge.Models;

namespace EnterBridge.Services;

public class EnterBridgeApiClient
{
    private readonly HttpClient _http;

    public EnterBridgeApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResult<Product>> GetProductsAsync(
        int page = 1, int pageSize = 20, string? category = null,
        string? search = null, string sortBy = "Name")
    {
        var url = $"api/products?pageNumber={page}&pageSize={pageSize}&sortBy={sortBy}";
        if (!string.IsNullOrEmpty(category))
            url += $"&category={Uri.EscapeDataString(category)}";
        if (!string.IsNullOrEmpty(search))
            url += $"&name={Uri.EscapeDataString(search)}";

        return await _http.GetFromJsonAsync<PagedResult<Product>>(url)
            ?? new PagedResult<Product>();
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        return await _http.GetFromJsonAsync<Product>($"api/products/{id}");
    }

    public async Task<PagedResult<PriceRecord>> GetPricesAsync(
        int productId, string? startDate = null, string? endDate = null,
        int page = 1, int pageSize = 200)
    {
        var url = $"api/prices?productId={productId}&pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(startDate))
            url += $"&startDate={startDate}";
        if (!string.IsNullOrEmpty(endDate))
            url += $"&endDate={endDate}";

        return await _http.GetFromJsonAsync<PagedResult<PriceRecord>>(url)
            ?? new PagedResult<PriceRecord>();
    }

    public async Task<List<PriceRecord>> GetAllPricesAsync(int productId, string? startDate = null, string? endDate = null)
    {
        var all = new List<PriceRecord>();
        int page = 1;
        PagedResult<PriceRecord> result;
        do
        {
            result = await GetPricesAsync(productId, startDate, endDate, page, 200);
            all.AddRange(result.Items);
            page++;
        } while (result.HasNextPage);
        return all;
    }

    public async Task<PriceRecord?> GetLatestPriceAsync(int productId)
    {
        var result = await GetPricesAsync(productId, pageSize: 1);
        return result.Items.FirstOrDefault();
    }

    public async Task<ApiStats?> GetStatsAsync()
    {
        return await _http.GetFromJsonAsync<ApiStats>("api/stats");
    }
}
