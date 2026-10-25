using System.Text.RegularExpressions;

namespace Foodblog;

// Shared helpers for building share/SEO metadata across content types (recipes, articles, pages).
public static class Seo
{
    private static readonly Regex BbcodeTag = new(@"\[/?[^\]]*\]", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    // Make a stored (usually root-relative) asset path absolute against the request origin,
    // leaving already-absolute URLs untouched.
    public static string Absolute(string baseUrl, string path) =>
        path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : $"{baseUrl}{path}";

    // Strip BBCode markup and collapse whitespace into a short plain-text snippet, suitable for a
    // meta description / share card. Truncated so cards stay readable.
    public static string PlainTextSnippet(string text, int maxLength = 200)
    {
        var stripped = BbcodeTag.Replace(text, string.Empty);
        var collapsed = Whitespace.Replace(stripped, " ").Trim();
        return collapsed.Length > maxLength ? collapsed[..(maxLength - 1)].TrimEnd() + "…" : collapsed;
    }
}
