namespace AmeWatches.Api.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = default!;
    public string AccessToken { get; set; } = default!;

    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }

    public string CustomerName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string Address { get; set; } = default!;
    public string City { get; set; } = default!;

    public decimal Subtotal { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }

    public DateTime? WarrantyUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
