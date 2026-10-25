namespace FunctionalBlog.Theme;

public static class ThemeHandlers
{
    public static App SetTheme => request => _ =>
    {
        var theme = Themes.Normalize(request.Form.GetValueOrDefault("theme", Themes.Default));
        var referer = request.Headers.GetValueOrDefault("Referer", "/");
        return ValueTask.FromResult(
            Response.Redirect(referer)
                .WithCookie(new Cookie("theme", theme, CookieSameSite.Lax)));
    };
}
