namespace FunctionalBlog;

// The only way to set a cookie (Response.WithCookie takes a Cookie, not a header string), so every
// cookie gets the same baseline: site-wide Path and Secure. Name and value must be cookie-safe.
public sealed record Cookie(string Name, string Value, CookieSameSite SameSite, bool HttpOnly = false)
{
    public Option<DateTimeOffset> Expires { get; init; }

    public Option<int> MaxAge { get; init; }

    public Cookie Expired() =>
        this with { Value = string.Empty, Expires = Option<DateTimeOffset>.None, MaxAge = Option.Some(0) };

    public string ToHeader() =>
        string.Join("; ", Attributes());

    private IEnumerable<string> Attributes()
    {
        yield return $"{Name}={Value}";
        yield return "Path=/";

        foreach (var expires in Expires.ToEnumerable())
        {
            yield return $"Expires={expires:R}";
        }

        foreach (var maxAge in MaxAge.ToEnumerable())
        {
            yield return $"Max-Age={maxAge}";
        }

        if (HttpOnly)
        {
            yield return "HttpOnly";
        }

        yield return $"SameSite={SameSite}";
        yield return "Secure";
    }
}
