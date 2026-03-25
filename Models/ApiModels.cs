namespace EnterBridge.Models;

public class PagedResult<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public List<T> Items { get; set; } = [];
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Category { get; set; } = "";
}

public class PriceRecord
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "";
    public int ProductId { get; set; }
    public Product? Product { get; set; }
}

public class ApiStats
{
    public int TotalProducts { get; set; }
    public Dictionary<string, int> ProductsByCategory { get; set; } = [];
    public int TotalPrices { get; set; }
    public PriceDataRange? PriceDataRange { get; set; }
    public List<string> UnitOfMeasures { get; set; } = [];
}

public class PriceDataRange
{
    public string Earliest { get; set; } = "";
    public string Latest { get; set; } = "";
}
