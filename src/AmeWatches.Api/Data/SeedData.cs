using AmeWatches.Api.Common;
using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await ApplyAsync(db);
    }

    public static async Task ApplyAsync(AppDbContext db)
    {
        if (await db.Brands.AnyAsync())
        {
            return;
        }

        var casio = new Brand { Name = "Casio", Slug = "casio" };
        var qq = new Brand { Name = "Q&Q", Slug = "q-and-q" };
        db.Brands.AddRange(casio, qq);

        var now = DateTime.UtcNow;

        Product Watch(Brand brand, string name, string code, decimal price, WatchType type,
                      string description, params string[] images)
        {
            var product = new Product
            {
                Brand = brand,
                Name = name,
                Slug = Slug.From($"{brand.Name} {name} {code}"),
                ModelCode = code,
                Price = price,
                Stock = 5,
                Gender = WatchGender.Women,
                Type = type,
                Movement = "Kuarc",
                Description = description,
                IsActive = true,
                CreatedAt = now
            };

            for (var i = 0; i < images.Length; i++)
            {
                product.Images.Add(new ProductImage { Url = $"/products/{images[i]}", SortOrder = i });
            }

            return product;
        }

        db.Products.AddRange(
            Watch(qq, "Classic Gold", "C09A-012PY", 55m, WatchType.Analog,
                "Orë klasike për gra me kuadrant të bardhë, numra arabë të qartë dhe byzylyk metalik ngjyrë ari.",
                "qq-c09a-012py-1.webp", "qq-c09a-012py-2.webp"),
            Watch(qq, "Pearl Mesh", "C50A-006PY", 55m, WatchType.Analog,
                "Kuadrant sedefi me numra arabë dhe rrip mesh ngjyrë ari, i lehtë dhe elegant.",
                "qq-c50a-006py-1.webp", "qq-c50a-006py-2.webp"),
            Watch(qq, "Noir Rectangle", "QQ-RECT-BLACK", 55m, WatchType.Analog,
                "Kasë drejtkëndëshe ari me kuadrant të zi, shifra romake dhe dy kristale mbi logo.",
                "qq-rect-black-1.webp", "qq-rect-black-2.webp"),
            Watch(qq, "Rectangle White", "QQ-RECT-WHITE", 55m, WatchType.Analog,
                "Kasë drejtkëndëshe ari me kuadrant të bardhë dhe tregues të artë, e hollë dhe e përditshme.",
                "qq-rect-white-1.webp"),
            Watch(casio, "Rectangle Gold", "CASIO-RECT-GOLD", 70m, WatchType.Analog,
                "Kasë drejtkëndëshe e artë me kuadrant të bardhë dhe tregues të hollë, me mekanizëm japonez.",
                "casio-rect-white-1.webp", "casio-rect-white-2.webp")
        );

        await db.SaveChangesAsync();
    }
}
