namespace AmeWatches.Api.Data.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public string ProductName { get; set; } = default!;
    public string ModelCode { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
