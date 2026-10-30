using AmeWatches.Api.Models.Catalog;

namespace AmeWatches.Api.Services;

public interface ICatalogService
{
    Task<List<BrandResponse>> GetBrands();
    Task<PagedResult<ProductListItem>> GetProducts(ProductQuery query);
    Task<ProductDetail?> GetProductBySlug(string slug);
}
