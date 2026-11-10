using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services;

public class CatalogService : ICatalogService
{
    private readonly AppDbContext _db;
    public CatalogService(AppDbContext db) => _db = db;

    public async Task<List<BrandResponse>> GetBrands()
        => await _db.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandResponse(b.Id, b.Name, b.Slug, b.Products.Count(p => p.IsActive)))
            .ToListAsync();

    public async Task<List<CategoryResponse>> GetCategories(string? brandSlug)
    {
        // Kategoritë pa asnjë orë aktive nuk shfaqen te klientët (do të çonin në faqe bosh).
        var categories = _db.Categories.AsNoTracking().Where(c => c.Products.Any(p => p.IsActive));
        if (!string.IsNullOrWhiteSpace(brandSlug))
            categories = categories.Where(c => c.Brand.Slug == brandSlug);

        return await categories
            .OrderBy(c => c.Brand.Name).ThenBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Slug, c.Brand.Slug, c.Products.Count(p => p.IsActive)))
            .ToListAsync();
    }

    public async Task<PagedResult<ProductListItem>> GetProducts(ProductQuery query)
    {
        var products = Filter(_db.Products.AsNoTracking().Where(p => p.IsActive), query);

        var total = await products.CountAsync();

        var items = await Sort(products, query.Sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductListItem(
                p.Id,
                p.Name,
                p.Slug,
                p.ModelCode,
                p.Brand.Name,
                p.Brand.Slug,
                p.Price,
                p.Stock > 0,
                p.Gender.ToString(),
                p.Type.ToString(),
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).Skip(1).FirstOrDefault(),
                p.Category != null ? p.Category.Name : null,
                p.Category != null ? p.Category.Slug : null))
            .ToListAsync();

        return new PagedResult<ProductListItem>(items, query.Page, query.PageSize, total);
    }

    public async Task<ProductDetail?> GetProductBySlug(string slug)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Slug == slug)
            .Select(p => new
            {
                p.Id, p.Name, p.Slug, p.ModelCode,
                Brand = p.Brand.Name, BrandSlug = p.Brand.Slug,
                p.Description, p.Price, p.Stock, p.Gender, p.Type,
                p.CaseSizeMm, p.WaterResistance, p.Movement,
                Category = p.Category != null ? p.Category.Name : null,
                CategorySlug = p.Category != null ? p.Category.Slug : null,
                Images = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
            })
            .FirstOrDefaultAsync();

        if (product is null)
            return null;

        return new ProductDetail(
            product.Id, product.Name, product.Slug, product.ModelCode,
            product.Brand, product.BrandSlug, product.Description, product.Price,
            product.Stock, product.Stock > 0, product.Gender.ToString(), product.Type.ToString(),
            product.CaseSizeMm, product.WaterResistance, product.Movement, product.Images,
            product.Category, product.CategorySlug);
    }

    public async Task<PriceRange> GetPriceRange()
    {
        var prices = _db.Products.AsNoTracking().Where(p => p.IsActive).Select(p => p.Price);
        if (!await prices.AnyAsync())
            return new PriceRange(0, 0);

        return new PriceRange(await prices.MinAsync(), await prices.MaxAsync());
    }

    public async Task<List<ProductSuggestion>> Suggest(string query, int limit = 6)
    {
        var term = query.Trim();
        if (term.Length == 0)
            return [];

        var contains = $"%{EscapeLike(term)}%";
        var prefix = $"{EscapeLike(term)}%";

        return await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && (
                EF.Functions.Like(p.Name, contains, "\\") ||
                EF.Functions.Like(p.ModelCode, contains, "\\") ||
                EF.Functions.Like(p.Brand.Name, contains, "\\")))
            .OrderBy(p => EF.Functions.Like(p.Name, prefix, "\\") ||
                          EF.Functions.Like(p.ModelCode, prefix, "\\") ||
                          EF.Functions.Like(p.Brand.Name, prefix, "\\") ? 0 : 1)
            .ThenBy(p => p.Name)
            .Take(limit)
            .Select(p => new ProductSuggestion(
                p.Name, p.Brand.Name, p.ModelCode, p.Slug, p.Price,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()))
            .ToListAsync();
    }

    private static IQueryable<Product> Filter(IQueryable<Product> products, ProductQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Brand))
            products = products.Where(p => p.Brand.Slug == query.Brand);

        if (!string.IsNullOrWhiteSpace(query.Category))
            products = products.Where(p => p.Category != null && p.Category.Slug == query.Category);

        if (query.MinPrice is decimal min)
            products = products.Where(p => p.Price >= min);

        if (query.MaxPrice is decimal max)
            products = products.Where(p => p.Price <= max);

        if (query.Gender is WatchGender gender)
            products = products.Where(p => p.Gender == gender);

        if (query.Type is WatchType type)
            products = products.Where(p => p.Type == type);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            products = products.Where(p =>
                EF.Functions.Like(p.Name, pattern, "\\") ||
                EF.Functions.Like(p.ModelCode, pattern, "\\") ||
                EF.Functions.Like(p.Brand.Name, pattern, "\\"));
        }

        return products;
    }

    private static string EscapeLike(string text)
        => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    private static IQueryable<Product> Sort(IQueryable<Product> products, ProductSort sort) => sort switch
    {
        ProductSort.PriceAsc => products.OrderBy(p => p.Price).ThenBy(p => p.Id),
        ProductSort.PriceDesc => products.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
        ProductSort.Name => products.OrderBy(p => p.Name).ThenBy(p => p.Id),
        _ => products.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
    };
}
