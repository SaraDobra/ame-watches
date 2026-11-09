using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Models.Catalog;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class CategoryTests
{
    private static async Task<(TestDb db, AdminCategoryService categories, AdminProductService products, Brand casio, Brand qq)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var casio = await db.Context.Brands.SingleAsync(b => b.Slug == "casio");
        var qq = await db.Context.Brands.SingleAsync(b => b.Slug == "q-and-q");
        return (db, new AdminCategoryService(db.Context), new AdminProductService(db.Context), casio, qq);
    }

    private static ProductInput Input(int brandId, int? categoryId, string code = "CAT-001")
        => new("Prova", code, brandId, null, 60m, 3, WatchGender.Men, WatchType.Analog, 40, 50, "Kuarc", true, categoryId);

    [Fact]
    public async Task Create_BuildsASlug_AndRefusesADuplicateNameInTheSameBrand()
    {
        var (db, categories, _, casio, qq) = await Setup();
        using var _ = db;

        var created = await categories.Create(new CategoryInput("  G-SHOCK ", casio.Id));
        Assert.True(created.Succeeded);
        Assert.Equal("G-SHOCK", created.Value!.Name);
        Assert.Equal("g-shock", created.Value.Slug);

        Assert.Equal(AdminError.Conflict, (await categories.Create(new CategoryInput("G-SHOCK", casio.Id))).Error);
        Assert.True((await categories.Create(new CategoryInput("G-SHOCK", qq.Id))).Succeeded);
    }

    [Fact]
    public async Task Create_ForAnUnknownBrand_IsInvalid()
    {
        var (db, categories, _, _, _) = await Setup();
        using var _ = db;

        Assert.Equal(AdminError.Invalid, (await categories.Create(new CategoryInput("EDIFICE", 999_999))).Error);
    }

    [Fact]
    public async Task Rename_ChangesNameAndSlug_ButNotToAnExistingName()
    {
        var (db, categories, _, casio, _) = await Setup();
        using var _ = db;
        var a = (await categories.Create(new CategoryInput("EDIFICE", casio.Id))).Value!;
        await categories.Create(new CategoryInput("G-SHOCK", casio.Id));

        var renamed = await categories.Rename(a.Id, "Edifice Sport");
        Assert.Equal("edifice-sport", renamed.Value!.Slug);

        Assert.Equal(AdminError.Conflict, (await categories.Rename(a.Id, "G-SHOCK")).Error);
        Assert.Equal(AdminError.NotFound, (await categories.Rename(999_999, "X Y")).Error);
    }

    [Fact]
    public async Task Delete_IsRefusedWhileWatchesUseTheCategory_AndAllowedWhenEmpty()
    {
        var (db, categories, products, casio, _) = await Setup();
        using var _ = db;
        var category = (await categories.Create(new CategoryInput("EDIFICE", casio.Id))).Value!;
        var product = (await products.Create(Input(casio.Id, category.Id))).Value!;

        Assert.Equal(AdminError.Conflict, (await categories.Delete(category.Id)).Error);
        Assert.Single(await categories.List());

        await products.Update(product.Id, Input(casio.Id, null));
        Assert.True((await categories.Delete(category.Id)).Succeeded);
        Assert.Empty(await categories.List());
    }

    [Fact]
    public async Task Product_CannotUseACategoryOfAnotherBrand_OrAMissingOne()
    {
        var (db, categories, products, casio, qq) = await Setup();
        using var _ = db;
        var casioCategory = (await categories.Create(new CategoryInput("EDIFICE", casio.Id))).Value!;

        Assert.Equal(AdminError.Invalid, (await products.Create(Input(qq.Id, casioCategory.Id))).Error);
        Assert.Equal(AdminError.Invalid, (await products.Create(Input(casio.Id, 999_999))).Error);

        var ok = await products.Create(Input(casio.Id, casioCategory.Id));
        Assert.Equal("EDIFICE", ok.Value!.Category);
        Assert.Equal(casioCategory.Id, ok.Value.CategoryId);
    }

    [Fact]
    public async Task Catalog_FiltersByCategory_AndCountsOnlyActiveWatches()
    {
        var (db, categories, products, casio, _) = await Setup();
        using var _ = db;
        var edifice = (await categories.Create(new CategoryInput("EDIFICE", casio.Id))).Value!;
        var gshock = (await categories.Create(new CategoryInput("G-SHOCK", casio.Id))).Value!;
        await products.Create(Input(casio.Id, edifice.Id, "E-1"));
        await products.Create(Input(casio.Id, edifice.Id, "E-2") with { IsActive = false });
        await products.Create(Input(casio.Id, gshock.Id, "G-1"));
        var catalog = new CatalogService(db.Context);

        var result = await catalog.GetProducts(new ProductQuery { Category = "edifice" });
        Assert.Single(result.Items);
        Assert.Equal("EDIFICE", result.Items[0].Category);
        Assert.Equal("edifice", result.Items[0].CategorySlug);

        var list = await catalog.GetCategories("casio");
        Assert.Equal(new[] { ("EDIFICE", 1), ("G-SHOCK", 1) }, list.Select(c => (c.Name, c.ProductCount)));
        Assert.Empty(await catalog.GetCategories("q-and-q"));
    }

    [Fact]
    public async Task Catalog_HidesCategoriesWithoutActiveWatches_ButAdminStillListsThem()
    {
        var (db, categories, products, casio, _) = await Setup();
        using var _ = db;
        var empty = (await categories.Create(new CategoryInput("EDIFICE", casio.Id))).Value!;
        var hidden = (await categories.Create(new CategoryInput("G-SHOCK", casio.Id))).Value!;
        await products.Create(Input(casio.Id, hidden.Id, "H-1") with { IsActive = false });
        var catalog = new CatalogService(db.Context);

        Assert.Empty(await catalog.GetCategories(null));
        Assert.Equal(2, (await categories.List()).Count);

        await products.Create(Input(casio.Id, empty.Id, "V-1"));
        Assert.Equal(new[] { "EDIFICE" }, (await catalog.GetCategories(null)).Select(c => c.Name));
    }
}
