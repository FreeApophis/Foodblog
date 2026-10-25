using System.Globalization;
using System.Text;

namespace Foodblog.Domain.Tags;

public static class Slug
{
    // Builds a URL slug from arbitrary text, handling German, French and Italian spellings:
    //   1. German digraphs are expanded first (ä→ae ö→oe ü→ue ß→ss) — this must precede
    //      diacritic stripping, since "ü" maps to "ue", not "u".
    //   2. Remaining diacritics are removed via Unicode NFD decomposition (è→e, û→u, ç→c, …).
    //   3. The result is lowercased; every run of non [a-z0-9] collapses to a single hyphen,
    //      and leading/trailing hyphens are trimmed.
    // Examples: "süss" → "suess", "Crème brûlée" → "creme-brulee", "Tiramisù" → "tiramisu".
    // Empty/punctuation-only input falls back to "n-a" so a slug is never blank.
    public static string From(string text)
    {
        var expanded = text.Trim()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
            .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue")
            .Replace("ß", "ss");

        var decomposed = expanded.Normalize(NormalizationForm.FormD);
        var withoutMarks = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                withoutMarks.Append(ch);
            }
        }

        var normalized = withoutMarks.ToString().ToLowerInvariant();

        var slug = new StringBuilder(normalized.Length);
        var pendingHyphen = false;
        foreach (var ch in normalized)
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (pendingHyphen && slug.Length > 0)
                {
                    slug.Append('-');
                }

                slug.Append(ch);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = true;
            }
        }

        return slug.Length == 0 ? "n-a" : slug.ToString();
    }
}
