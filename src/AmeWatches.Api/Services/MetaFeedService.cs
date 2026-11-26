using System.Globalization;
using System.Text;
using AmeWatches.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services;

public record FeedRow(
    string Id, string Brand, string Name, string? Description, string Slug,
    decimal Price, decimal? OldPrice, bool InStock, IReadOnlyList<string> Images);

public interface IMetaFeedService
{
    Task<string> BuildCsv(string baseUrl);
}

/// <summary>Lista e orëve për Meta Commerce Manager (Facebook/Instagram Shopping), në format CSV.</summary>
public class MetaFeedService : IMetaFeedService
{
    private const string Header = "id,title,description,availability,condition,price,sale_price,link,image_link,additional_image_link,brand";
    private readonly AppDbContext _db;

    public MetaFeedService(AppDbContext db) => _db = db;

    public async Task<string> BuildCsv(string baseUrl)
    {
        // Vetëm orët aktive me të paktën një foto (Meta e kërkon image_link).
        var rows = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Images.Any())
            .OrderBy(p => p.Id)
            .Select(p => new FeedRow(
                p.ModelCode, p.Brand.Name, p.Name, p.Description, p.Slug,
                p.Price, p.OldPrice, p.Stock > 0,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()))
            .ToListAsync();

        return Format(rows, baseUrl);
    }

    public static string Format(IEnumerable<FeedRow> rows, string baseUrl)
    {
        var csv = new StringBuilder(Header).Append("\r\n");
        foreach (var r in rows)
        {
            var title = $"{r.Brand} {r.Name} {r.Id}";
            var description = string.IsNullOrWhiteSpace(r.Description)
                ? $"{title} me garanci 1 vit nga amé watches dhe dërgesë falas."
                : r.Description.Trim();

            // Në zbritje: price = çmimi i vjetër, sale_price = çmimi aktual (si e kërkon Meta).
            var onSale = r.OldPrice is decimal old && old > r.Price;
            var price = Money(onSale ? r.OldPrice!.Value : r.Price);
            var salePrice = onSale ? Money(r.Price) : "";

            var extra = string.Join(",", r.Images.Skip(1).Take(10).Select(i => Absolute(baseUrl, i)));

            csv.Append(string.Join(",", new[]
            {
                Field(r.Id),
                Field(Cut(title, 200)),
                Field(Cut(description, 9999)),
                Field(r.InStock ? "in stock" : "out of stock"),
                Field("new"),
                Field(price),
                Field(salePrice),
                Field(Absolute(baseUrl, $"/ora/{r.Slug}")),
                Field(Absolute(baseUrl, r.Images[0])),
                Field(extra),
                Field(r.Brand)
            })).Append("\r\n");
        }
        return csv.ToString();
    }

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture) + " EUR";

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..max];

    private static string Absolute(string baseUrl, string url)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;
        return baseUrl.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url);
    }

    /// <summary>Fusha CSV: nëse ka presje, thonjëza ose rresht të ri, mbyllet në thonjëza.</summary>
    private static string Field(string value)
        => value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
