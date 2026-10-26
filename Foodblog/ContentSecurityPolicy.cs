namespace Foodblog;

// Adds a Content-Security-Policy to every HTML response. All scripts are same-origin files (no
// inline scripts or event handlers; the JSON-LD block is data, not executed). Inline styles stay
// allowed for the object-position/avatar style attributes and htmx's indicator styles. Images may
// come from https: because editors can embed external image URLs (BBCode [img], ingredient images).
// Instagram may be framed for the home page's two-click embed (which only creates the iframe after
// consent), but only while an Instagram handle is configured.
public static class ContentSecurityPolicy
{
    public static string Policy(bool allowInstagram) =>
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; " +
        "font-src 'self'; connect-src 'self'; " +
        (allowInstagram ? "frame-src https://www.instagram.com; " : string.Empty) +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'";

    public static Middleware Create() => next => request => async env =>
    {
        var response = await next(request)(env);
        var allowInstagram = env.Config?.InstagramHandle is [_];

        return response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
            ? response.WithHeader("Content-Security-Policy", Policy(allowInstagram))
            : response;
    };
}
