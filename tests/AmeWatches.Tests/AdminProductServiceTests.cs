using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Models.Catalog;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class AdminProductServiceTests
{
    private static async Task<(TestDb db, AdminProductService service)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        return (db, new AdminProductService(db.Context));
    }

    private static ProductInput Input(int brandId, string name = "Prova Klasike", string code = "TEST-001", decimal price = 60m, int stock = 4, bool active = true)
        => new(name, code, brandId, "Përshkrim i shkurtër", price, stock, WatchGender.Women, WatchType.Analog, 30, 50, "Kuarc", active);

    [Fact]
    public async Task Create_BuildsAUniqueReadableSlug()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var brand = await db.Context.Brands.SingleAsync(b => b.Slug == "q-and-q");

        var first = await service.Create(Input(brand.Id));
        var second = await service.Create(Input(brand.Id, code: "TEST-002"));

        Assert.True(first.Succeeded);
        Assert.Equal("q-and-q-prova-klasike-test-001", first.Value!.Slug);
        Assert.Equal("q-and-q-prova-klasike-test-002", second.Value!.Slug);
    }

    [Fact]
    public async Task Create_WithADuplicateModelCode_IsAConflict()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var existing = await db.Context.Products.FirstAsync();

        var result = await service.Create(Input(existing.BrandId, code: existing.ModelCode));

        Assert.Equal(AdminError.Conflict, result.Error);
    }

    [Fact]
    public async Task Create_WithAnUnknownBrand_IsInvalid()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.Create(Input(9999));

        Assert.Equal(AdminError.Invalid, result.Error);
        Assert.Equal(12, await db.Context.Products.CountAsync());
    }

    [Fact]
    public async Task Update_ChangesTheDataButKeepsTheSlug()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var brand = await db.Context.Brands.FirstAsync();
        var created = (await service.Create(Input(brand.Id))).Value!;

        var updated = await service.Update(created.Id, Input(brand.Id, name: "Emër i ri krejt", price: 99m, stock: 9));

        Assert.True(updated.Succeeded);
        Assert.Equal("Emër i ri krejt", updated.Value!.Name);
        Assert.Equal(99m, updated.Value.Price);
        Assert.Equal(created.Slug, updated.Value.Slug);
    }

    [Fact]
    public async Task Update_CannotTakeAnotherProductsModelCode_ButCanKeepItsOwn()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var brand = await db.Context.Brands.FirstAsync();
        var created = (await service.Create(Input(brand.Id))).Value!;
        var other = await db.Context.Products.FirstAsync(p => p.Id != created.Id);

        var clash = await service.Update(created.Id, Input(brand.Id, code: other.ModelCode));
        var same = await service.Update(created.Id, Input(brand.Id, price: 70m));

        Assert.Equal(AdminError.Conflict, clash.Error);
        Assert.True(same.Succeeded);
    }

    [Fact]
    public async Task Update_UnknownProduct_IsNotFound()
    {
        var (db, service) = await Setup();
        using var _ = db;

        Assert.Equal(AdminError.NotFound, (await service.Update(9999, Input(1))).Error);
        Assert.Equal(AdminError.NotFound, (await service.SetActive(9999, false)).Error);
    }

    [Fact]
    public async Task Deactivating_HidesTheWatchFromTheShop_ButNotFromTheAdminList()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();

        await service.SetActive(product.Id, false);

        var shop = await new CatalogService(db.Context).GetProducts(new ProductQuery { PageSize = 48 });
        Assert.DoesNotContain(shop.Items, p => p.Id == product.Id);

        var admin = await service.List(new AdminProductQuery { PageSize = 100 });
        Assert.Contains(admin.Items, p => p.Id == product.Id && !p.IsActive);
        Assert.Equal(12, admin.TotalCount);
    }

    [Fact]
    public async Task List_CanFilterByActiveState_AndSearchByCode()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        await service.SetActive(product.Id, false);

        var inactive = await service.List(new AdminProductQuery { IsActive = false });
        var search = await service.List(new AdminProductQuery { Search = "efr-556" });
        var wildcard = await service.List(new AdminProductQuery { Search = "%" });

        Assert.Equal(product.Id, Assert.Single(inactive.Items).Id);
        Assert.Equal("EFR-556D-1AV", Assert.Single(search.Items).ModelCode);
        Assert.Equal(0, wildcard.TotalCount);
    }

    [Fact]
    public async Task Blank_OptionalFields_AreStoredAsNull()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var brand = await db.Context.Brands.FirstAsync();
        var input = Input(brand.Id) with { Description = "   ", Movement = "  ", CaseSizeMm = null, WaterResistanceM = null };

        var created = (await service.Create(input)).Value!;

        Assert.Null(created.Description);
        Assert.Null(created.Movement);
        Assert.Null(created.CaseSizeMm);
    }
}
