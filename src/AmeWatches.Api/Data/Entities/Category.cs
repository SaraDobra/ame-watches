namespace AmeWatches.Api.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = default!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
