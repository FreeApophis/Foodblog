namespace Foodblog.Theme;

public static class ThemeMiddleware
{
    public static Middleware Create() => next => request => env =>
    {
        var theme = Themes.Normalize(request.Cookies.GetValueOrNone("theme").GetOrElse(Themes.Default));

        return next(request)(env with { Theme = theme });
    };
}
