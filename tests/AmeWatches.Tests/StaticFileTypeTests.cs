using Microsoft.AspNetCore.StaticFiles;

namespace AmeWatches.Tests;

public class StaticFileTypeTests
{
    [Theory]
    [InlineData("a.jpg", "image/jpeg")]
    [InlineData("a.png", "image/png")]
    [InlineData("a.webp", "image/webp")]
    [InlineData("a.avif", "image/avif")]
    public void UploadedPhotos_AreServedWithTheRightContentType(string file, string expected)
    {
        Assert.True(new FileExtensionContentTypeProvider().TryGetContentType(file, out var type));
        Assert.Equal(expected, type);
    }
}
