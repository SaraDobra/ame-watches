using System.Text;
using System.Text.RegularExpressions;

namespace AmeWatches.Api.Common;

public static partial class Slug
{
    public static string From(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var lower = sb.ToString().ToLowerInvariant().Replace("&", " and ");
        return Dashes().Replace(NonAlphanumeric().Replace(lower, "-"), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex("-{2,}")]
    private static partial Regex Dashes();
}
