using AmeWatches.Api.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Models.Storage;
using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace AmeWatches.Tests;

public class ImageUploadTests : IDisposable
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 2, 3];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 1, 2, 3];
    private static readonly byte[] Webp = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 1, 2, 3, 4, (byte)'W', (byte)'E', (byte)'B', (byte)'P', 9, 9];
    private static readonly byte[] Html = "<html><script>alert(1)</script></html>"u8.ToArray();

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ame-uploads-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "t";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private async Task<(TestDb db, AdminImageService service, LocalImageStorage storage, int productId)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var storage = new LocalImageStorage(Options.Create(new StorageOptions { UploadsPath = _folder }), new FakeEnvironment());
        var service = new AdminImageService(db.Context, storage, new AdminProductService(db.Context));
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        return (db, service, storage, product.Id);
    }

    private static UploadedFile File(string name, byte[] bytes, long? length = null)
        => new(name, length ?? bytes.Length, () => new MemoryStream(bytes));

    [Theory]
    [InlineData("jpg")]
    [InlineData("png")]
    [InlineData("webp")]
    public void TheValidator_RecognisesRealImages(string expected)
    {
        var bytes = expected switch { "jpg" => Jpeg, "png" => Png, _ => Webp };
        Assert.Equal(expected, ImageValidator.DetectExtension(bytes));
    }

    [Fact]
    public void TheValidator_RejectsHtmlEmptyAndTruncatedData()
    {
        Assert.Null(ImageValidator.DetectExtension(Html));
        Assert.Null(ImageValidator.DetectExtension([]));
        Assert.Null(ImageValidator.DetectExtension([0xFF, 0xD8]));
        Assert.Null(ImageValidator.DetectExtension([(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'A', (byte)'V', (byte)'I', (byte)' ']));
    }

    [Fact]
    public async Task Add_StoresFilesWithRandomNames_AndAppendsAfterTheExistingPhotos()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;
        db.Context.ProductImages.Add(new ProductImage { ProductId = productId, Url = "/products/old.webp", SortOrder = 0 });
        await db.Context.SaveChangesAsync();

        var result = await service.Add(productId, [File("foto-ime.jpg", Jpeg), File("tjeter.png", Png)]);

        Assert.True(result.Succeeded);
        var images = result.Value!.Images;
        Assert.Equal(3, images.Count);
        Assert.Equal(new[] { 0, 1, 2 }, images.Select(i => i.SortOrder));
        Assert.Matches("^/uploads/[0-9a-f]{32}\\.jpg$", images[1].Url);
        Assert.Matches("^/uploads/[0-9a-f]{32}\\.png$", images[2].Url);
        Assert.DoesNotContain("foto-ime", images[1].Url);
        Assert.Equal(2, Directory.GetFiles(_folder).Length);
    }

    [Fact]
    public async Task Add_RejectsADisguisedFile_AndSavesNothing()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;

        var result = await service.Add(productId, [File("ok.jpg", Jpeg), File("shume-mire.jpg", Html)]);

        Assert.Equal(AdminError.Invalid, result.Error);
        Assert.False(Directory.Exists(_folder) && Directory.GetFiles(_folder).Length > 0);
        Assert.Equal(0, await db.Context.ProductImages.CountAsync(i => i.ProductId == productId));
    }

    [Fact]
    public async Task Add_RejectsEmptyAndOversizedFiles()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;

        var empty = await service.Add(productId, [File("a.jpg", [], 0)]);
        var huge = await service.Add(productId, [File("b.jpg", Jpeg, ImageValidator.MaxBytes + 1)]);
        var none = await service.Add(productId, []);

        Assert.Equal(AdminError.Invalid, empty.Error);
        Assert.Equal(AdminError.Invalid, huge.Error);
        Assert.Equal(AdminError.Invalid, none.Error);
    }

    [Fact]
    public async Task Add_StopsAtEightPhotos()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;

        Assert.True((await service.Add(productId, Enumerable.Range(0, 8).Select(i => File($"{i}.jpg", Jpeg)).ToList())).Succeeded);
        var ninth = await service.Add(productId, [File("9.jpg", Jpeg)]);

        Assert.Equal(AdminError.Invalid, ninth.Error);
        Assert.Equal(8, await db.Context.ProductImages.CountAsync(i => i.ProductId == productId));
    }

    [Fact]
    public async Task Add_ToAnUnknownProduct_IsNotFound()
    {
        var (db, service, _, _) = await Setup();
        using var _ = db;

        Assert.Equal(AdminError.NotFound, (await service.Add(9999, [File("a.jpg", Jpeg)])).Error);
    }

    [Fact]
    public async Task Delete_RemovesTheRowAndTheFile()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;
        var added = (await service.Add(productId, [File("a.jpg", Jpeg)])).Value!;
        var image = added.Images.Single();

        var result = await service.Delete(productId, image.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Images);
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task Delete_OfASeedPhoto_NeverTouchesTheDisk()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;
        Directory.CreateDirectory(_folder);
        var bystander = Path.Combine(_folder, "qq-c09a-012py-1.webp");
        await System.IO.File.WriteAllBytesAsync(bystander, Webp);
        var seed = new ProductImage { ProductId = productId, Url = "/products/qq-c09a-012py-1.webp", SortOrder = 0 };
        db.Context.ProductImages.Add(seed);
        await db.Context.SaveChangesAsync();

        await service.Delete(productId, seed.Id);

        Assert.True(System.IO.File.Exists(bystander));
    }

    [Theory]
    [InlineData("/uploads/../secret.jpg")]
    [InlineData("/uploads/..%2Fsecret.jpg")]
    [InlineData("/uploads/abc.jpg")]
    [InlineData("/etc/passwd")]
    [InlineData("/uploads/0123456789abcdef0123456789abcdef.exe")]
    public async Task Storage_Delete_IgnoresAnythingThatIsNotOneOfItsOwnFiles(string url)
    {
        var (db, _, storage, _) = await Setup();
        using var _ = db;
        Directory.CreateDirectory(_folder);
        var parent = Path.GetDirectoryName(_folder)!;
        var outside = Path.Combine(parent, "secret.jpg");
        await System.IO.File.WriteAllBytesAsync(outside, Jpeg);

        try
        {
            await storage.Delete(url);
            Assert.True(System.IO.File.Exists(outside));
        }
        finally
        {
            System.IO.File.Delete(outside);
        }
    }

    [Fact]
    public async Task Delete_WrongProductOrImage_IsNotFound()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;
        var image = (await service.Add(productId, [File("a.jpg", Jpeg)])).Value!.Images.Single();
        var otherProduct = await db.Context.Products.FirstAsync(p => p.Id != productId);

        Assert.Equal(AdminError.NotFound, (await service.Delete(otherProduct.Id, image.Id)).Error);
        Assert.Equal(AdminError.NotFound, (await service.Delete(productId, 9999)).Error);
    }

    [Fact]
    public async Task Reorder_ChangesThePrimaryPhoto()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;
        var images = (await service.Add(productId, [File("a.jpg", Jpeg), File("b.png", Png), File("c.jpg", Jpeg)])).Value!.Images;

        var result = await service.Reorder(productId, [images[2].Id, images[0].Id, images[1].Id]);

        Assert.Equal(new[] { images[2].Id, images[0].Id, images[1].Id }, result.Value!.Images.Select(i => i.Id));
        Assert.Equal(new[] { 0, 1, 2 }, result.Value.Images.Select(i => i.SortOrder));
    }

    [Fact]
    public async Task Reorder_RequiresExactlyTheProductsOwnPhotos()
    {
        var (db, service, _, productId) = await Setup();
        using var _ = db;
        var images = (await service.Add(productId, [File("a.jpg", Jpeg), File("b.png", Png)])).Value!.Images;

        Assert.Equal(AdminError.Invalid, (await service.Reorder(productId, [images[0].Id])).Error);
        Assert.Equal(AdminError.Invalid, (await service.Reorder(productId, [images[0].Id, images[0].Id])).Error);
        Assert.Equal(AdminError.Invalid, (await service.Reorder(productId, [images[0].Id, 9999])).Error);
    }
}
