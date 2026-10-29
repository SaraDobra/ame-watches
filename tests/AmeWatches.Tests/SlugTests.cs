using AmeWatches.Api.Common;

namespace AmeWatches.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("Casio Edifice EFR-556D-1AV", "casio-edifice-efr-556d-1av")]
    [InlineData("Q&Q Superior", "q-and-q-superior")]
    [InlineData("  Ora   të  bukura  ", "ora-te-bukura")]
    [InlineData("amé watches", "ame-watches")]
    public void From_ProducesUrlSafeText(string input, string expected)
        => Assert.Equal(expected, Slug.From(input));
}
