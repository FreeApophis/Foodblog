using System.Text.Json;

namespace FunctionalBlog.Translations;

public static class TranslationHandlers
{
    public static App List => _ => async env =>
    {
        var all = env.Translations is not null ? await env.Translations.All() : [];
        return Response.Html(TranslationViews.List(all, env.Ctx));
    };

    public static App Save(string encodedKey, string language) => request => async env =>
    {
        if (env.Translations is null)
        {
            return Response.NotFound();
        }

        var key = Uri.UnescapeDataString(encodedKey);
        var text = request.Form.GetValueOrDefault("text", string.Empty);
        await env.Translations.Save(key, language, null, text);

        if (env.TranslationCache is not null)
        {
            await env.TranslationCache.RefreshAsync();
        }

        return Response.Redirect("/admin/translations");
    };

    public static App Export => _ => async env =>
    {
        var all = env.Translations is not null ? await env.Translations.All() : [];

        var grouped = all
            .GroupBy(t => t.Key)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(t => t.Language, t => t.Text));

        var json = JsonSerializer.Serialize(grouped, new JsonSerializerOptions { WriteIndented = true });
        return Response.JsonDownload("translations.json", json);
    };

    public static App SetLanguage => request => _ =>
    {
        var lang = request.Form.GetValueOrDefault("lang", Languages.Default);
        var safe = Languages.Supported.Contains(lang) ? lang : Languages.Default;
        var referer = request.Headers.GetValueOrDefault("Referer", "/");
        return ValueTask.FromResult(
            Response.Redirect(referer)
                .WithCookie(new Cookie("lang", safe, CookieSameSite.Lax)));
    };
}
