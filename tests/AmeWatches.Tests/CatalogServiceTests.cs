using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Catalog;
using AmeWatches.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class CatalogServiceTests
{
    private static async Task<(TestDb db, CatalogService service)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        return (db, new CatalogService(db.Context));
    }

    [Fact]
    public async Task Brands_ReturnBothWithTheirProductCount()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var brands = await service.GetBrands();

        Assert.Equal(new[] { "Casio", "Q&Q" }, brands.Select(b => b.Name));
        Assert.All(brands, b => Assert.Equal(6, b.ProductCount));
    }

    [Fact]
    public async Task Brands_DoNotCountInactiveProducts()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var hidden = await db.Context.Products.FirstAsync(p => p.Brand.Slug == "casio");
        hidden.IsActive = false;
        await db.Context.SaveChangesAsync();

        var casio = (await service.GetBrands()).Single(b => b.Slug == "casio");

        Assert.Equal(5, casio.ProductCount);
    }

    [Fact]
    public async Task Products_DefaultQuery_ReturnsFirstPageOfTwelve()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.GetProducts(new ProductQuery());

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(12, result.Items.Count);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task Products_FilterByBrand_ReturnsOnlyThatBrand()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.GetProducts(new ProductQuery { Brand = "q-and-q" });

        Assert.Equal(6, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal("Q&Q", p.Brand));
    }

    [Fact]
    public async Task Products_FilterByPriceRange_IncludesBoundaries()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.GetProducts(new ProductQuery { MinPrice = 65m, MaxPrice = 79m });

        Assert.All(result.Items, p => Assert.InRange(p.Price, 65m, 79m));
        Assert.Contains(result.Items, p => p.Price == 65m);
        Assert.Contains(result.Items, p => p.Price == 79m);
    }

    [Fact]
    public async Task Products_FilterByGenderAndType_CombineWithAnd()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.GetProducts(new ProductQuery { Gender = WatchGender.Women, Type = WatchType.Analog });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p =>
        {
            Assert.Equal("Women", p.Gender);
            Assert.Equal("Analog", p.Type);
        });
    }

    [Fact]
    public async Task Products_SearchFindsByModelCode()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.GetProducts(new ProductQuery { Search = "efr-556" });

        var only = Assert.Single(result.Items);
        Assert.Equal("EFR-556D-1AV", only.ModelCode);
    }

    [Fact]
    public async Task Products_SortByPrice_AscendingAndDescending()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var asc = (await service.GetProducts(new ProductQuery { Sort = ProductSort.PriceAsc })).Items.Select(p => p.Price).ToList();
        var desc = (await service.GetProducts(new ProductQuery { Sort = ProductSort.PriceDesc })).Items.Select(p => p.Price).ToList();

        Assert.Equal(asc.OrderBy(x => x), asc);
        Assert.Equal(desc.OrderByDescending(x => x), desc);
        Assert.Equal(52m, asc.First());
        Assert.Equal(189m, desc.First());
    }

    [Fact]
    public async Task Products_Paging_SplitsWithoutOverlap()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var first = await service.GetProducts(new ProductQuery { PageSize = 5, Page = 1, Sort = ProductSort.PriceAsc });
        var third = await service.GetProducts(new ProductQuery { PageSize = 5, Page = 3, Sort = ProductSort.PriceAsc });
        var second = await service.GetProducts(new ProductQuery { PageSize = 5, Page = 2, Sort = ProductSort.PriceAsc });

        Assert.Equal(12, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(5, first.Items.Count);
        Assert.Equal(2, third.Items.Count);
        Assert.Empty(first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)));
    }

    [Fact]
    public async Task Products_InactiveAreHidden()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var hidden = await db.Context.Products.FirstAsync();
        hidden.IsActive = false;
        await db.Context.SaveChangesAsync();

        var result = await service.GetProducts(new ProductQuery());

        Assert.Equal(11, result.TotalCount);
        Assert.DoesNotContain(result.Items, p => p.Id == hidden.Id);
        Assert.Null(await service.GetProductBySlug(hidden.Slug));
    }

    [Fact]
    public async Task ProductBySlug_ReturnsDetailWithSpecs()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var detail = await service.GetProductBySlug("casio-edifice-efr-556d-1av");

        Assert.NotNull(detail);
        Assert.Equal("Casio", detail.Brand);
        Assert.Equal(44, detail.CaseSizeMm);
        Assert.Equal(100, detail.WaterResistanceM);
        Assert.True(detail.InStock);
    }

    [Fact]
    public async Task ProductBySlug_UnknownSlug_ReturnsNull()
    {
        var (db, service) = await Setup();
        using var _ = db;

        Assert.Null(await service.GetProductBySlug("nuk-ekziston"));
    }

    [Fact]
    public async Task Products_OutOfStock_IsMarkedNotInStock()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        product.Stock = 0;
        await db.Context.SaveChangesAsync();

        var result = await service.GetProducts(new ProductQuery { Search = product.ModelCode });

        Assert.False(result.Items.Single().InStock);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[a]")]
    public async Task Products_SearchWithWildcardCharacters_IsTreatedAsPlainText(string term)
    {
        var (db, service) = await Setup();
        using var _ = db;

        var result = await service.GetProducts(new ProductQuery { Search = term });

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task PriceRange_ReturnsTheLowestAndHighestActivePrice()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var range = await service.GetPriceRange();

        Assert.Equal(52m, range.Min);
        Assert.Equal(189m, range.Max);
    }

    [Fact]
    public async Task PriceRange_IgnoresHiddenWatches_AndIsZeroForAnEmptyShop()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var priciest = await db.Context.Products.OrderByDescending(p => p.Price).FirstAsync();
        priciest.IsActive = false;
        await db.Context.SaveChangesAsync();

        Assert.Equal(145m, (await service.GetPriceRange()).Max);

        foreach (var product in await db.Context.Products.ToListAsync()) product.IsActive = false;
        await db.Context.SaveChangesAsync();

        Assert.Equal(new PriceRange(0, 0), await service.GetPriceRange());
    }

    [Fact]
    public async Task Suggest_FindsByNameCodeAndBrand_AsSoonAsOneLetterIsTyped()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var byLetter = await service.Suggest("e");
        var byCode = await service.Suggest("efr");
        var byBrand = await service.Suggest("q&q");

        Assert.NotEmpty(byLetter);
        Assert.Equal("EFR-556D-1AV", Assert.Single(byCode).ModelCode);
        Assert.Equal(6, byBrand.Count);
        Assert.All(byBrand, s => Assert.Equal("Q&Q", s.Brand));
    }

    [Fact]
    public async Task Suggest_PutsThoseThatStartWithTheLettersFirst_AndRespectsTheLimit()
    {
        var (db, service) = await Setup();
        using var _ = db;

        var results = await service.Suggest("s", limit: 3);

        Assert.Equal(3, results.Count);
        Assert.StartsWith("S", results[0].Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Suggest_SkipsHiddenWatches_BlankInput_AndWildcards()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var hidden = await db.Context.Products.FirstAsync(p => p.ModelCode == "EFR-556D-1AV");
        hidden.IsActive = false;
        await db.Context.SaveChangesAsync();

        Assert.Empty(await service.Suggest("efr"));
        Assert.Empty(await service.Suggest("   "));
        Assert.Empty(await service.Suggest("%"));
        Assert.Empty(await service.Suggest("_"));
    }
}
