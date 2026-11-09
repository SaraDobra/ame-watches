using AmeWatches.Api.Common;
using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services.Admin;

public interface IAdminCategoryService
{
    Task<List<AdminCategory>> List();
    Task<AdminResult<AdminCategory>> Create(CategoryInput input);
    Task<AdminResult<AdminCategory>> Rename(int id, string name);
    Task<AdminResult<int>> Delete(int id);
}

public class AdminCategoryService : IAdminCategoryService
{
    private readonly AppDbContext _db;
    public AdminCategoryService(AppDbContext db) => _db = db;

    public async Task<List<AdminCategory>> List()
        => await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Brand.Name).ThenBy(c => c.Name)
            .Select(c => new AdminCategory(c.Id, c.Name, c.Slug, c.BrandId, c.Brand.Name, c.Products.Count))
            .ToListAsync();

    public async Task<AdminResult<AdminCategory>> Create(CategoryInput input)
    {
        var brand = await _db.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == input.BrandId);
        if (brand is null)
            return AdminResult<AdminCategory>.Invalid("Brendi nuk ekziston.");

        var name = input.Name.Trim();
        if (await NameTaken(brand.Id, name, null))
            return AdminResult<AdminCategory>.Conflict($"Kategoria \"{name}\" ekziston tashmë te {brand.Name}.");

        var category = new Category { Name = name, BrandId = brand.Id, Slug = await UniqueSlug(name, null) };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return AdminResult<AdminCategory>.Ok(new AdminCategory(category.Id, category.Name, category.Slug, brand.Id, brand.Name, 0));
    }

    public async Task<AdminResult<AdminCategory>> Rename(int id, string newName)
    {
        var category = await _db.Categories.Include(c => c.Brand).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return AdminResult<AdminCategory>.NotFound("Kategoria nuk u gjet.");

        var name = newName.Trim();
        if (await NameTaken(category.BrandId, name, id))
            return AdminResult<AdminCategory>.Conflict($"Kategoria \"{name}\" ekziston tashmë te {category.Brand.Name}.");

        category.Name = name;
        category.Slug = await UniqueSlug(name, id);
        await _db.SaveChangesAsync();

        var count = await _db.Products.CountAsync(p => p.CategoryId == id);
        return AdminResult<AdminCategory>.Ok(new AdminCategory(category.Id, category.Name, category.Slug, category.BrandId, category.Brand.Name, count));
    }

    public async Task<AdminResult<int>> Delete(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return AdminResult<int>.NotFound("Kategoria nuk u gjet.");

        var count = await _db.Products.CountAsync(p => p.CategoryId == id);
        if (count > 0)
            return AdminResult<int>.Conflict($"Kjo kategori ka {count} orë, prandaj nuk mund të fshihet. Ndrysho kategorinë e atyre orëve para.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return AdminResult<int>.Ok(id);
    }

    private Task<bool> NameTaken(int brandId, string name, int? exceptId)
        => _db.Categories.AnyAsync(c => c.BrandId == brandId && c.Name == name && c.Id != exceptId);

    private async Task<string> UniqueSlug(string name, int? exceptId)
    {
        var baseSlug = Slug.From(name);
        if (baseSlug.Length == 0)
            baseSlug = "kategori";

        var slug = baseSlug;
        var counter = 2;
        while (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != exceptId))
            slug = $"{baseSlug}-{counter++}";
        return slug;
    }
}
