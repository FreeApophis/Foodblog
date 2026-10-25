namespace FunctionalBlog;

// Adds a Content-Security-Policy to every HTML response. All scripts are same-origin files (no
// inline scripts or event handlers; the JSON-LD block is data, not executed). Inline styles stay
// allowed for the object-position/avatar style attributes and htmx's indicator styles. Images may
// come from https: because editors can embed external image URLs (BBCode [img], ingredient images).
public static class ContentSecurityPolicy
{
    public const string Policy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'";

    public static Middleware Create() => next => request => async env =>
    {
        var response = await next(request)(env);

        return response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
            ? response.WithHeader("Content-Security-Policy", Policy)
            : response;
    };
}
