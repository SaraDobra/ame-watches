namespace AmeWatches.Api.Services.Storage;

public interface IImageStorage
{
    Task<string> Save(Stream content, string extension, CancellationToken cancellationToken = default);
    Task Delete(string url);
}
