namespace FunctionalBlog.Test;

public sealed class CookieTests
{
    [Fact]
    public void A_cookie_is_always_secure_and_site_wide()
    {
        var header = new Cookie("theme", "dark", CookieSameSite.Lax).ToHeader();

        Assert.Equal("theme=dark; Path=/; SameSite=Lax; Secure", header);
    }

    [Fact]
    public void An_http_only_cookie_with_an_expiry_renders_all_attributes()
    {
        var expires = new DateTimeOffset(2026, 11, 24, 8, 0, 0, TimeSpan.Zero);

        var header = new Cookie("session", "abc", CookieSameSite.Strict, HttpOnly: true) { Expires = Option.Some(expires) }.ToHeader();

        Assert.Equal("session=abc; Path=/; Expires=Tue, 24 Nov 2026 08:00:00 GMT; HttpOnly; SameSite=Strict; Secure", header);
    }

    [Fact]
    public void An_expired_cookie_is_cleared_but_keeps_its_attributes()
    {
        var header = new Cookie("session", "abc", CookieSameSite.Strict, HttpOnly: true).Expired().ToHeader();

        Assert.Equal("session=; Path=/; Max-Age=0; HttpOnly; SameSite=Strict; Secure", header);
    }
}
