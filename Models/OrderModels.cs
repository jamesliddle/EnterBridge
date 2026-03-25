using System.ComponentModel.DataAnnotations;

namespace EnterBridge.Models;

public enum OrderStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

public class Order
{
    public int Id { get; set; }

    [Required]
    public string SubmittedBy { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Submitted;

    public string? Notes { get; set; }
    public string? ReviewedBy { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public decimal Total => Items.Where(i => !i.IsRejected).Sum(i => i.LineTotal);
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductSku { get; set; } = "";
    public string Category { get; set; } = "";

    public decimal UnitPrice { get; set; }
    public string UnitOfMeasure { get; set; } = "";
    public int Quantity { get; set; } = 1;

    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;

    public Order? Order { get; set; }
}
