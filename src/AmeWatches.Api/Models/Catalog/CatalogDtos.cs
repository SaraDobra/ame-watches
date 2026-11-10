using System.ComponentModel.DataAnnotations;
using AmeWatches.Api.Data.Entities;

namespace AmeWatches.Api.Models.Catalog;

public enum ProductSort
{
    Newest = 1,
    PriceAsc = 2,
    PriceDesc = 3,
    Name = 4
}

public class ProductQuery
{
    public string? Brand { get; set; }
    public string? Category { get; set; }

    [Range(0, 100000)]
    public decimal? MinPrice { get; set; }

    [Range(0, 100000)]
    public decimal? MaxPrice { get; set; }

    public WatchGender? Gender { get; set; }
    public WatchType? Type { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    public ProductSort Sort { get; set; } = ProductSort.Newest;

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 48)]
    public int PageSize { get; set; } = 12;
}

public record BrandResponse(int Id, string Name, string Slug, int ProductCount);

public record CategoryResponse(int Id, string Name, string Slug, string BrandSlug, int ProductCount);

public record ProductListItem(
    int Id,
    string Name,
    string Slug,
    string ModelCode,
    string Brand,
    string BrandSlug,
    decimal Price,
    bool InStock,
    string Gender,
    string Type,
    string? ImageUrl,
    string? SecondImageUrl,
    string? Category = null,
    string? CategorySlug = null
);

public record ProductDetail(
    int Id,
    string Name,
    string Slug,
    string ModelCode,
    string Brand,
    string BrandSlug,
    string? Description,
    decimal Price,
    int Stock,
    bool InStock,
    string Gender,
    string Type,
    decimal? CaseSizeMm,
    string? WaterResistance,
    string? Movement,
    IReadOnlyList<string> Images,
    string? Category = null,
    string? CategorySlug = null
);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record PriceRange(decimal Min, decimal Max);

public record ProductSuggestion(string Name, string Brand, string ModelCode, string Slug, decimal Price, string? ImageUrl);
