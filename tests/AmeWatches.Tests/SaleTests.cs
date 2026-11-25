using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Models.Catalog;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class SaleTests
{
    private static ProductInput Input(int brandId, decimal price, decimal? oldPrice)
        => new("Prova", "SALE-001", brandId, null, price, 3, WatchGender.Men, WatchType.Analog, 40, "50 m", "Kuarc", true, null, oldPrice);

    private static async Task<(TestDb db, AdminProductService products, int brandId)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var casio = await db.Context.Brands.SingleAsync(b => b.Slug == "casio");
        return (db, new AdminProductService(db.Context), casio.Id);
    }

    [Fact]
    public async Task OldPrice_MustBeGreaterThanThePrice()
    {
        var (db, products, brandId) = await Setup();
        using var _ = db;

        Assert.Equal(AdminError.Invalid, (await products.Create(Input(brandId, 60m, 60m))).Error);
        Assert.Equal(AdminError.Invalid, (await products.Create(Input(brandId, 60m, 50m))).Error);
    }

    [Fact]
    public async Task Sale_IsSaved_AndShownOnThePublicCatalog_AndCanBeCleared()
    {
        var (db, products, brandId) = await Setup();
        using var _ = db;

        var created = (await products.Create(Input(brandId, 60m, 80m))).Value!;
        Assert.Equal(80m, created.OldPrice);

        var catalog = new CatalogService(db.Context);
        var listed = (await catalog.GetProducts(new ProductQuery { Search = "SALE-001" })).Items.Single();
        Assert.Equal(80m, listed.OldPrice);
        Assert.Equal(60m, listed.Price);
        Assert.Equal(80m, (await catalog.GetProductBySlug(created.Slug))!.OldPrice);

        var cleared = (await products.Update(created.Id, Input(brandId, 60m, null))).Value!;
        Assert.Null(cleared.OldPrice);
    }
}
