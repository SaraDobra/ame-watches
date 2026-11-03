using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services.Admin;

public record UploadedFile(string FileName, long Length, Func<Stream> Open);

public interface IAdminImageService
{
    Task<AdminResult<AdminProductDetail>> Add(int productId, IReadOnlyList<UploadedFile> files, CancellationToken cancellationToken = default);
    Task<AdminResult<AdminProductDetail>> Delete(int productId, int imageId);
    Task<AdminResult<AdminProductDetail>> Reorder(int productId, IReadOnlyList<int> imageIds);
}

public class AdminImageService : IAdminImageService
{
    public const int MaxImages = 8;

    private readonly AppDbContext _db;
    private readonly IImageStorage _storage;
    private readonly IAdminProductService _products;

    public AdminImageService(AppDbContext db, IImageStorage storage, IAdminProductService products)
    {
        _db = db;
        _storage = storage;
        _products = products;
    }

    public async Task<AdminResult<AdminProductDetail>> Add(int productId, IReadOnlyList<UploadedFile> files, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
            return AdminResult<AdminProductDetail>.NotFound("Produkti nuk u gjet.");

        if (files.Count == 0)
            return AdminResult<AdminProductDetail>.Invalid("Zgjidhni të paktën një foto.");

        if (product.Images.Count + files.Count > MaxImages)
            return AdminResult<AdminProductDetail>.Invalid($"Një orë mund të ketë deri në {MaxImages} foto.");

        var extensions = new List<string>();
        foreach (var file in files)
        {
            if (file.Length is 0 or > ImageValidator.MaxBytes)
                return AdminResult<AdminProductDetail>.Invalid($"\"{file.FileName}\" duhet të jetë deri në 5 MB.");

            var header = new byte[ImageValidator.HeaderLength];
            await using (var stream = file.Open())
            {
                var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
                var extension = ImageValidator.DetectExtension(header.AsSpan(0, read));
                if (extension is null)
                    return AdminResult<AdminProductDetail>.Invalid($"\"{file.FileName}\" nuk është foto e vlefshme (JPG, PNG ose WebP).");

                extensions.Add(extension);
            }
        }

        var nextOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1;
        for (var i = 0; i < files.Count; i++)
        {
            await using var stream = files[i].Open();
            var url = await _storage.Save(stream, extensions[i], cancellationToken);
            product.Images.Add(new ProductImage { Url = url, SortOrder = nextOrder++ });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return AdminResult<AdminProductDetail>.Ok((await _products.Get(productId))!);
    }

    public async Task<AdminResult<AdminProductDetail>> Delete(int productId, int imageId)
    {
        var image = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);
        if (image is null)
            return AdminResult<AdminProductDetail>.NotFound("Fotoja nuk u gjet.");

        var url = image.Url;
        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();
        await _storage.Delete(url);

        return AdminResult<AdminProductDetail>.Ok((await _products.Get(productId))!);
    }

    public async Task<AdminResult<AdminProductDetail>> Reorder(int productId, IReadOnlyList<int> imageIds)
    {
        var images = await _db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();
        if (images.Count == 0 && !await _db.Products.AnyAsync(p => p.Id == productId))
            return AdminResult<AdminProductDetail>.NotFound("Produkti nuk u gjet.");

        var requested = imageIds.ToHashSet();
        if (imageIds.Count != requested.Count || requested.Count != images.Count || !images.All(i => requested.Contains(i.Id)))
            return AdminResult<AdminProductDetail>.Invalid("Lista duhet të përmbajë saktësisht të gjitha fotot e orës, pa dublikata.");

        for (var position = 0; position < imageIds.Count; position++)
            images.Single(i => i.Id == imageIds[position]).SortOrder = position;

        await _db.SaveChangesAsync();
        return AdminResult<AdminProductDetail>.Ok((await _products.Get(productId))!);
    }
}
