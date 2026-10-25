namespace FunctionalBlog.Configuration;

// The public base URL used for absolute links in emails (password reset, email verification).
// Only a plain absolute https:// URL is accepted. Links are never derived from the request's Host
// header, which an attacker controls (host-header poisoning of reset links).
public static class SiteUrl
{
    public static Option<string> ParseOrNone(string raw)
    {
        var trimmed = raw.Trim().TrimEnd('/');

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Length > 0
            && uri.UserInfo.Length == 0
            && uri.Query.Length == 0
            && uri.Fragment.Length == 0
                ? Option.Some(trimmed)
                : Option<string>.None;
    }
}
