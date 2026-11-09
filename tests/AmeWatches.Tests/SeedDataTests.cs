using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class SeedDataTests
{
    [Fact]
    public async Task Seed_CreatesTwoBrands()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        var names = await db.Context.Brands.Select(b => b.Name).OrderBy(n => n).ToListAsync();
        Assert.Equal(new[] { "Casio", "Q&Q" }, names);
    }

    [Fact]
    public async Task Seed_PricesStayInsideTheBusinessRange()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        Assert.True(await db.Context.Products.AllAsync(p => p.Price >= 50m && p.Price <= 300m));
    }

    [Fact]
    public async Task Seed_RunTwice_DoesNotDuplicate()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);
        var first = await db.Context.Products.CountAsync();

        await SeedData.ApplyAsync(db.Context);

        Assert.Equal(first, await db.Context.Products.CountAsync());
    }

    [Fact]
    public async Task Seed_EveryProductHasSlugAndModelCode()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        var products = await db.Context.Products.ToListAsync();
        Assert.All(products, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Slug));
            Assert.False(string.IsNullOrWhiteSpace(p.ModelCode));
        });
        Assert.Equal(products.Count, products.Select(p => p.Slug).Distinct().Count());
    }

    [Fact]
    public async Task ModelCode_MustBeUnique()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);
        var brand = await db.Context.Brands.FirstAsync();
        var existing = await db.Context.Products.FirstAsync();

        db.Context.Products.Add(new Product
        {
            Name = "Dublikat",
            Slug = "dublikat",
            ModelCode = existing.ModelCode,
            BrandId = brand.Id,
            Price = 60m,
            CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Order_ItemKeepsItsOwnPrice_WhenProductPriceChanges()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);
        var product = await db.Context.Products.FirstAsync();
        var paid = product.Price;

        var order = new Order
        {
            OrderNo = "T-1",
            AccessToken = "token",
            Status = OrderStatus.New,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Pending,
            CustomerName = "Test",
            Phone = "049000000",
            Address = "Rruga 1",
            City = "Prishtinë",
            Subtotal = paid,
            Total = paid,
            CreatedAt = DateTime.UtcNow,
            Items = { new OrderItem { ProductId = product.Id, ProductName = product.Name, ModelCode = product.ModelCode, UnitPrice = paid, Quantity = 1 } }
        };
        db.Context.Orders.Add(order);
        await db.Context.SaveChangesAsync();

        product.Price = paid + 50m;
        await db.Context.SaveChangesAsync();

        var item = await db.Context.OrderItems.SingleAsync();
        Assert.Equal(paid, item.UnitPrice);
    }

    [Fact]
    public async Task Seed_PricesMatchTheBusinessList()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        var products = await db.Context.Products.Include(p => p.Brand).ToListAsync();
        Assert.All(products.Where(p => p.Brand.Slug == "q-and-q"), p => Assert.Equal(55m, p.Price));
        Assert.All(products.Where(p => p.Brand.Slug == "casio"), p => Assert.Equal(70m, p.Price));
    }

    [Fact]
    public async Task Seed_EveryProductHasAtLeastOnePhoto_AndTheFileExists()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        var publicDir = FindPublicDirectory();
        var products = await db.Context.Products.Include(p => p.Images).ToListAsync();

        Assert.All(products, p =>
        {
            Assert.NotEmpty(p.Images);
            Assert.All(p.Images, image => Assert.True(File.Exists(Path.Combine(publicDir, image.Url.TrimStart('/'))), image.Url));
        });
    }

    private static string FindPublicDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "frontend", "public")))
            dir = dir.Parent;

        return dir is null
            ? throw new DirectoryNotFoundException("frontend/public nuk u gjet.")
            : Path.Combine(dir.FullName, "frontend", "public");
    }

    [Fact]
    public async Task Seed_HasBothMenAndWomenWatches()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        Assert.True(await db.Context.Products.AnyAsync(p => p.Gender == WatchGender.Men));
        Assert.True(await db.Context.Products.AnyAsync(p => p.Gender == WatchGender.Women));
    }

    [Fact]
    public async Task Seed_AutomaticWatchIsMarkedAsMenAndAutomatic()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);

        var watch = await db.Context.Products.SingleAsync(p => p.ModelCode == "CMK-100TC-1A");

        Assert.Equal(WatchGender.Men, watch.Gender);
        Assert.Equal("Automatik", watch.Movement);
        Assert.Equal(3, await db.Context.ProductImages.CountAsync(i => i.ProductId == watch.Id));
    }

    [Fact]
    public async Task Catalog_ListItems_ExposeTheFirstAndSecondPhoto()
    {
        using var db = new TestDb();
        await SeedData.ApplyAsync(db.Context);
        var service = new AmeWatches.Api.Services.CatalogService(db.Context);

        var items = (await service.GetProducts(new AmeWatches.Api.Models.Catalog.ProductQuery { PageSize = 48 })).Items;

        var automatic = items.Single(i => i.ModelCode == "CMK-100TC-1A");
        Assert.EndsWith("casio-cmk-100tc-1a-1.png", automatic.ImageUrl);
        Assert.EndsWith("casio-cmk-100tc-1a-2.jpg", automatic.SecondImageUrl);
        Assert.Null(items.Single(i => i.ModelCode == "QQ-RECT-WHITE").SecondImageUrl);
    }
}
