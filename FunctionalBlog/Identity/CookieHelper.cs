namespace FunctionalBlog.Identity;

public static class CookieHelper
{
    public static Cookie SessionCookie(string token, DateTimeOffset expires) =>
        new Cookie("session", token, CookieSameSite.Strict, HttpOnly: true) { Expires = Option.Some(expires) };

    public static Cookie ExpireSessionCookie() =>
        new Cookie("session", string.Empty, CookieSameSite.Strict, HttpOnly: true).Expired();
}
