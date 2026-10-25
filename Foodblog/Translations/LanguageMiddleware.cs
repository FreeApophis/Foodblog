namespace Foodblog.Translations;

public static class LanguageMiddleware
{
    public static Middleware Create() => next => request => env =>
    {
        var lang = request.Cookies.GetValueOrNone("lang").GetOrElse(Languages.Default);

        return next(request)(env with { Language = lang });
    };
}
