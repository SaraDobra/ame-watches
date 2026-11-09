using AmeWatches.Api.Models.Catalog;

namespace AmeWatches.Api.Services;

public interface ICatalogService
{
    Task<List<BrandResponse>> GetBrands();
    Task<List<CategoryResponse>> GetCategories(string? brandSlug);
    Task<PagedResult<ProductListItem>> GetProducts(ProductQuery query);
    Task<ProductDetail?> GetProductBySlug(string slug);
    Task<PriceRange> GetPriceRange();
    Task<List<ProductSuggestion>> Suggest(string query, int limit = 6);
}
