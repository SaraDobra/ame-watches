using System.Text.RegularExpressions;
using AmeWatches.Api.Models.Storage;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Storage;

public partial class LocalImageStorage : IImageStorage
{
    public const string UrlPrefix = "/uploads/";

    private readonly string _root;

    public LocalImageStorage(IOptions<StorageOptions> options, IWebHostEnvironment environment)
    {
        var configured = options.Value.UploadsPath;
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "uploads")
            : Path.GetFullPath(configured);
    }

    public string Root => _root;

    public async Task<string> Save(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);

        var fileName = $"{Guid.NewGuid():N}.{extension}";
        await using var file = File.Create(Path.Combine(_root, fileName));
        await content.CopyToAsync(file, cancellationToken);

        return UrlPrefix + fileName;
    }

    public Task Delete(string url)
    {
        if (!url.StartsWith(UrlPrefix, StringComparison.Ordinal))
            return Task.CompletedTask;

        var name = url[UrlPrefix.Length..];
        if (!SafeName().IsMatch(name))
            return Task.CompletedTask;

        var path = Path.Combine(_root, name);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|png|webp)$")]
    private static partial Regex SafeName();
}
