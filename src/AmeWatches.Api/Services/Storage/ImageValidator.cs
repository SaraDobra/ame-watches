namespace AmeWatches.Api.Services.Storage;

public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int HeaderLength = 12;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "jpg";

        if (header.Length >= 8 && header[..8].SequenceEqual(PngSignature))
            return "png";

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "webp";

        return null;
    }
}
