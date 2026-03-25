using EnterBridge.Models;

namespace EnterBridge.Services;

public class CartItem
{
    public Product Product { get; set; } = null!;
    public PriceRecord Price { get; set; } = null!;
    public int Quantity { get; set; } = 1;
    public decimal LineTotal => Price.Amount * Quantity;
}

public class CartService
{
    private readonly List<CartItem> _items = [];
    public IReadOnlyList<CartItem> Items => _items;
    public int Count => _items.Count;
    public decimal Total => _items.Sum(i => i.LineTotal);

    public event Action? OnChange;

    public void AddItem(Product product, PriceRecord price, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new CartItem { Product = product, Price = price, Quantity = quantity });
        }
        OnChange?.Invoke();
    }

    public void RemoveItem(int productId)
    {
        _items.RemoveAll(i => i.Product.Id == productId);
        OnChange?.Invoke();
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.Product.Id == productId);
        if (item != null)
        {
            if (quantity <= 0)
                _items.Remove(item);
            else
                item.Quantity = quantity;
        }
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _items.Clear();
        OnChange?.Invoke();
    }
}
