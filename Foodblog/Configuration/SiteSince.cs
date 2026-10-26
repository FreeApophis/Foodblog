using System.Globalization;

namespace Foodblog.Configuration;

// The four-digit year the site went online, shown in the utility bar ("seit 2016").
public static class SiteSince
{
    public static Option<int> ParseOrNone(string raw)
    {
        var trimmed = raw.Trim();

        return trimmed.Length == 4 && int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            ? Option.Some(year)
            : Option<int>.None;
    }
}
