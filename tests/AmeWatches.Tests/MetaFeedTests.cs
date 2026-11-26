using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class MetaFeedTests
{
    private const string Base = "https://amewatches.com";

    private static FeedRow Row(
        string id = "LTP-1", string name = "Classic", string? description = "Orë klasike",
        decimal price = 55m, decimal? oldPrice = null, bool inStock = true, params string[] images)
        => new(id, "Casio", name, description, "casio-classic-ltp-1", price, oldPrice, inStock,
            images.Length > 0 ? images : ["/products/a.webp"]);

    private static string[] Lines(string csv) => csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Header_HasTheColumnsMetaRequires()
    {
        var header = Lines(MetaFeedService.Format([], Base))[0].Split(',');
        foreach (var column in new[] { "id", "title", "description", "availability", "condition", "price", "link", "image_link", "brand" })
            Assert.Contains(column, header);
    }

    [Fact]
    public void Row_HasFullUrls_PriceWithCurrency_AndAvailability()
    {
        var line = Lines(MetaFeedService.Format([Row()], Base))[1];

        Assert.Equal(
            "LTP-1,Casio Classic LTP-1,Orë klasike,in stock,new,55.00 EUR,,https://amewatches.com/ora/casio-classic-ltp-1,https://amewatches.com/products/a.webp,,Casio",
            line);
    }

    [Fact]
    public void SoldOutWatch_IsMarkedOutOfStock()
        => Assert.Contains(",out of stock,", Lines(MetaFeedService.Format([Row(inStock: false)], Base))[1]);

    [Fact]
    public void WatchOnSale_PutsOldPriceInPrice_AndCurrentPriceInSalePrice()
        => Assert.Contains(",new,80.00 EUR,55.00 EUR,", Lines(MetaFeedService.Format([Row(price: 55m, oldPrice: 80m)], Base))[1]);

    [Fact]
    public void MissingDescription_GetsADefaultOne()
        => Assert.Contains("me garanci 1 vit", Lines(MetaFeedService.Format([Row(description: "  ")], Base))[1]);

    [Fact]
    public void CommasQuotesAndNewlines_AreEscapedSoTheyDoNotBreakTheColumns()
    {
        var csv = MetaFeedService.Format([Row(description: "Kasë 40 mm, \"ari\"\nme rrip")], Base);

        Assert.Contains("\"Kasë 40 mm, \"\"ari\"\"\nme rrip\"", csv);
    }

    [Fact]
    public void ExtraImages_GoInAdditionalImageLink_AbsoluteAndCommaSeparated()
    {
        var line = Lines(MetaFeedService.Format([Row(images: ["/products/a.webp", "/uploads/b.jpg", "https://cdn.example.com/c.png"])], Base))[1];

        Assert.Contains("\"https://amewatches.com/uploads/b.jpg,https://cdn.example.com/c.png\"", line);
    }

    [Fact]
    public async Task BuildCsv_IncludesOnlyActiveWatchesWithAPhoto()
    {
        using var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var casio = await db.Context.Brands.SingleAsync(b => b.Slug == "casio");
        var admin = new AdminProductService(db.Context);

        ProductInput Input(string code, bool active) => new("Prova", code, casio.Id, null, 60m, 3, WatchGender.Men, WatchType.Analog, 40, "50 m", "Kuarc", active);
        var withPhoto = (await admin.Create(Input("FEED-OK", true))).Value!;
        var noPhoto = (await admin.Create(Input("FEED-NOPHOTO", true))).Value!;
        var hidden = (await admin.Create(Input("FEED-HIDDEN", false))).Value!;
        foreach (var id in new[] { withPhoto.Id, hidden.Id })
            db.Context.ProductImages.Add(new ProductImage { ProductId = id, Url = $"/uploads/{id}.jpg", SortOrder = 0 });
        await db.Context.SaveChangesAsync();

        var csv = await new MetaFeedService(db.Context).BuildCsv(Base);

        Assert.Contains("FEED-OK,", csv);
        Assert.DoesNotContain("FEED-NOPHOTO", csv);
        Assert.DoesNotContain("FEED-HIDDEN", csv);
    }
}
