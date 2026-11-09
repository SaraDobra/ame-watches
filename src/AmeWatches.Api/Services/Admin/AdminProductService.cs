using AmeWatches.Api.Common;
using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services.Admin;

public interface IAdminProductService
{
    Task<PagedAdmin<AdminProductListItem>> List(AdminProductQuery query);
    Task<AdminProductDetail?> Get(int id);
    Task<AdminResult<AdminProductDetail>> Create(ProductInput input);
    Task<AdminResult<AdminProductDetail>> Update(int id, ProductInput input);
    Task<AdminResult<AdminProductDetail>> SetActive(int id, bool isActive);
    Task<AdminResult<int>> Delete(int id);
}

public class AdminProductService : IAdminProductService
{
    private readonly AppDbContext _db;
    private readonly IImageStorage? _storage;

    public AdminProductService(AppDbContext db, IImageStorage? storage = null)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PagedAdmin<AdminProductListItem>> List(AdminProductQuery query)
    {
        var products = _db.Products.AsNoTracking().AsQueryable();

        if (query.IsActive is bool active)
            products = products.Where(p => p.IsActive == active);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            products = products.Where(p =>
                EF.Functions.Like(p.Name, pattern, "\\") ||
                EF.Functions.Like(p.ModelCode, pattern, "\\") ||
                EF.Functions.Like(p.Brand.Name, pattern, "\\"));
        }

        var total = await products.CountAsync();

        var items = await products
            .OrderByDescending(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new AdminProductListItem(
                p.Id, p.Name, p.Slug, p.ModelCode, p.Brand.Name, p.Price, p.Stock, p.IsActive,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Category != null ? p.Category.Name : null))
            .ToListAsync();

        return new PagedAdmin<AdminProductListItem>(items, query.Page, query.PageSize, total);
    }

    public async Task<AdminProductDetail?> Get(int id)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product is null ? null : ToDetail(product);
    }

    public async Task<AdminResult<AdminProductDetail>> Create(ProductInput input)
    {
        var brand = await _db.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == input.BrandId);
        if (brand is null)
            return AdminResult<AdminProductDetail>.Invalid("Brendi nuk ekziston.");

        var categoryError = await CheckCategory(input);
        if (categoryError is not null)
            return AdminResult<AdminProductDetail>.Invalid(categoryError);

        var code = input.ModelCode.Trim();
        if (await _db.Products.AnyAsync(p => p.ModelCode == code))
            return AdminResult<AdminProductDetail>.Conflict($"Kodi i modelit \"{code}\" ekziston tashmë.");

        var product = new Product
        {
            Slug = await UniqueSlug(Slug.From($"{brand.Name} {input.Name} {code}")),
            CreatedAt = DateTime.UtcNow
        };
        Apply(product, input, code);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return AdminResult<AdminProductDetail>.Ok((await Get(product.Id))!);
    }

    public async Task<AdminResult<AdminProductDetail>> Update(int id, ProductInput input)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return AdminResult<AdminProductDetail>.NotFound("Produkti nuk u gjet.");

        if (!await _db.Brands.AnyAsync(b => b.Id == input.BrandId))
            return AdminResult<AdminProductDetail>.Invalid("Brendi nuk ekziston.");

        var categoryError = await CheckCategory(input);
        if (categoryError is not null)
            return AdminResult<AdminProductDetail>.Invalid(categoryError);

        var code = input.ModelCode.Trim();
        if (await _db.Products.AnyAsync(p => p.ModelCode == code && p.Id != id))
            return AdminResult<AdminProductDetail>.Conflict($"Kodi i modelit \"{code}\" ekziston tashmë.");

        Apply(product, input, code);
        await _db.SaveChangesAsync();

        return AdminResult<AdminProductDetail>.Ok((await Get(id))!);
    }

    public async Task<AdminResult<AdminProductDetail>> SetActive(int id, bool isActive)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return AdminResult<AdminProductDetail>.NotFound("Produkti nuk u gjet.");

        product.IsActive = isActive;
        await _db.SaveChangesAsync();
        return AdminResult<AdminProductDetail>.Ok((await Get(id))!);
    }

    public async Task<AdminResult<int>> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return AdminResult<int>.NotFound("Produkti nuk u gjet.");

        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id))
            return AdminResult<int>.Conflict("Kjo orë ka porosi, prandaj nuk mund të fshihet. Fshihe nga dyqani me çelësin \"Në dyqan\".");

        var urls = product.Images.Select(i => i.Url).ToList();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        // Skedarët fshihen vetëm pasi databaza ka ruajtur me sukses.
        if (_storage is not null)
            foreach (var url in urls)
                await _storage.Delete(url);

        return AdminResult<int>.Ok(id);
    }

    private async Task<string?> CheckCategory(ProductInput input)
    {
        if (input.CategoryId is not int categoryId)
            return null;

        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null)
            return "Kategoria nuk ekziston.";

        return category.BrandId == input.BrandId ? null : "Kategoria nuk i përket brendit të zgjedhur.";
    }

    private static void Apply(Product product, ProductInput input, string code)
    {
        product.Name = input.Name.Trim();
        product.ModelCode = code;
        product.BrandId = input.BrandId;
        product.CategoryId = input.CategoryId;
        product.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        product.Price = input.Price;
        product.Stock = input.Stock;
        product.Gender = input.Gender;
        product.Type = input.Type;
        product.CaseSizeMm = input.CaseSizeMm;
        product.WaterResistanceM = input.WaterResistanceM;
        product.Movement = string.IsNullOrWhiteSpace(input.Movement) ? null : input.Movement.Trim();
        product.IsActive = input.IsActive;
    }

    private async Task<string> UniqueSlug(string baseSlug)
    {
        var slug = baseSlug;
        var counter = 2;
        while (await _db.Products.AnyAsync(p => p.Slug == slug))
            slug = $"{baseSlug}-{counter++}";
        return slug;
    }

    private static AdminProductDetail ToDetail(Product p) => new(
        p.Id, p.Name, p.Slug, p.ModelCode, p.BrandId, p.Brand.Name, p.Description, p.Price, p.Stock,
        p.Gender.ToString(), p.Type.ToString(), p.CaseSizeMm, p.WaterResistanceM, p.Movement, p.IsActive,
        p.Images.OrderBy(i => i.SortOrder).Select(i => new AdminImage(i.Id, i.Url, i.SortOrder)).ToList(),
        p.CategoryId, p.Category?.Name);

    private static string EscapeLike(string text)
        => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
