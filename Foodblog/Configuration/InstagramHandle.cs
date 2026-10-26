using System.Text.RegularExpressions;

namespace Foodblog.Configuration;

// An Instagram account name as shown after the "@" (e.g. "the_cooking.couple_"). Only Instagram's
// own alphabet is accepted, so the handle can be dropped into profile and embed URLs as-is.
public static partial class InstagramHandle
{
    public static Option<string> ParseOrNone(string raw)
    {
        var handle = raw.Trim().TrimStart('@');

        return HandlePattern().IsMatch(handle)
            ? Option.Some(handle)
            : Option<string>.None;
    }

    [GeneratedRegex("^[A-Za-z0-9._]{1,30}$")]
    private static partial Regex HandlePattern();
}
