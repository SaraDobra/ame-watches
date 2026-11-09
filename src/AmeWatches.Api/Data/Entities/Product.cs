namespace AmeWatches.Api.Data.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string ModelCode { get; set; } = default!;

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = default!;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public WatchGender Gender { get; set; }
    public WatchType Type { get; set; }
    public int? CaseSizeMm { get; set; }
    public int? WaterResistanceM { get; set; }
    public string? Movement { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
