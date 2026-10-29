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

        Product Watch(Brand brand, string name, string code, decimal price, int stock,
                      WatchGender gender, WatchType type, int caseMm, int waterM, string movement, string description)
            => new()
            {
                Brand = brand,
                Name = name,
                Slug = Slug.From($"{brand.Name} {name} {code}"),
                ModelCode = code,
                Price = price,
                Stock = stock,
                Gender = gender,
                Type = type,
                CaseSizeMm = caseMm,
                WaterResistanceM = waterM,
                Movement = movement,
                Description = description,
                IsActive = true,
                CreatedAt = now
            };

        db.Products.AddRange(
            Watch(casio, "Edifice", "EFR-556D-1AV", 189m, 5, WatchGender.Men, WatchType.Analog, 44, 100, "Kuarc", "Orë sportive me kronograf dhe pamje elegante."),
            Watch(casio, "G-Shock", "GA-2100-1A1", 145m, 4, WatchGender.Men, WatchType.AnalogDigital, 45, 200, "Kuarc", "Kasë e hollë dhe rezistente ndaj goditjeve."),
            Watch(casio, "Vintage", "A168WA-1", 59m, 8, WatchGender.Unisex, WatchType.Digital, 38, 30, "Kuarc", "Klasikja dixhitale me stil retro."),
            Watch(casio, "Classic", "AE-1200WH-1A", 69m, 6, WatchGender.Men, WatchType.Digital, 42, 100, "Kuarc", "Dixhitale me orë botërore dhe alarm."),
            Watch(casio, "Standard", "MQ-24-7B", 55m, 10, WatchGender.Unisex, WatchType.Analog, 33, 30, "Kuarc", "Analoge e thjeshtë për përdorim të përditshëm."),
            Watch(casio, "Sport", "W-218H-1AV", 52m, 9, WatchGender.Men, WatchType.Digital, 43, 50, "Kuarc", "Dixhitale e lehtë me kronometër."),
            Watch(qq, "Superior", "QQ-S294J-002Y", 79m, 7, WatchGender.Men, WatchType.Analog, 40, 100, "Kuarc", "Analoge klasike me rrip të zi."),
            Watch(qq, "Attractive", "QQ-VS44J-001Y", 65m, 6, WatchGender.Women, WatchType.Analog, 34, 50, "Kuarc", "Orë e hollë për gra me kuadrant të bardhë."),
            Watch(qq, "Smart Solar", "QQ-M184J-003Y", 95m, 4, WatchGender.Men, WatchType.Analog, 42, 100, "Solar", "Lëviz me energji diellore, pa nevojë për bateri."),
            Watch(qq, "Gold Line", "QQ-C04A-005Y", 89m, 3, WatchGender.Women, WatchType.Analog, 30, 30, "Kuarc", "Kasë ngjyrë ari dhe rrip metalik."),
            Watch(qq, "Digital Sport", "QQ-M013J-004Y", 72m, 5, WatchGender.Unisex, WatchType.Digital, 44, 100, "Kuarc", "Dixhitale sportive me dritë."),
            Watch(qq, "Dress", "QQ-QZ00J-402Y", 85m, 2, WatchGender.Men, WatchType.Analog, 39, 50, "Kuarc", "Orë e zezë elegante për rastet formale.")
        );

        await db.SaveChangesAsync();
    }
}
