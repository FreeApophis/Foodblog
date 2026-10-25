namespace Foodblog.Units;

public static class AdminUnitHandlers
{
    public static App List => _ => async env =>
        Response.Html(AdminUnitViews.List(await env.Units.All(), env.Ctx));

    public static App NewRow => _ => env =>
        ValueTask.FromResult(Response.Html(AdminUnitViews.NewRow(env.Ctx)));

    // Cancelling a not-yet-saved add row simply removes it (empty swap).
    public static App CancelNew => _ => _ =>
        ValueTask.FromResult(Response.Html(string.Empty));

    // Cancelling an edit restores the read-only row.
    public static App Row(UnitId id) => _ => async env =>
        (await env.Units.Find(id)) is [var unit]
            ? Response.Html(AdminUnitViews.ViewRow(unit, env.Ctx))
            : Response.Html(string.Empty);

    public static App EditRow(UnitId id) => _ => async env =>
        (await env.Units.Find(id)) is [var unit]
            ? Response.Html(AdminUnitViews.EditRow(unit, env.Ctx))
            : Response.Html(string.Empty);

    public static App Create => request => async env =>
        await UnitForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                AdminUnitViews.NewRow(
                    request.Form.GetValueOrNone("name").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("abbreviation").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("category").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("factor").GetOrElse(string.Empty),
                    env.Ctx) +
                AdminUnitViews.ErrorOob(f.Error, env.T),
                400)),
            success: async s =>
            {
                var id = await env.Units.NextId();
                var nameKey = $"unit.{id.Value}.name";
                var abbrKey = $"unit.{id.Value}.abbr";
                var unit = new Unit(id, nameKey, abbrKey, s.Value.Category, s.Value.Factor);
                await env.Units.Save(unit);
                await SaveText(env, nameKey, s.Value.Name, allLanguages: true);
                await SaveText(env, abbrKey, s.Value.Abbreviation, allLanguages: true);
                await RefreshCache(env);
                return Response.Html(AdminUnitViews.ViewRow(unit, env.Ctx) + AdminUnitViews.ErrorOob([], env.T));
            });

    public static App Update(UnitId id) => request => async env =>
    {
        if ((await env.Units.Find(id)) is not [var existing])
        {
            return Response.Html(string.Empty);
        }

        return await UnitForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                AdminUnitViews.EditRow(
                    id,
                    request.Form.GetValueOrNone("name").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("abbreviation").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("category").GetOrElse(string.Empty),
                    request.Form.GetValueOrNone("factor").GetOrElse(string.Empty),
                    env.Ctx) +
                AdminUnitViews.ErrorOob(f.Error, env.T),
                400)),
            success: async s =>
            {
                var unit = new Unit(id, existing.NameKey, existing.AbbreviationKey, s.Value.Category, s.Value.Factor);
                await env.Units.Save(unit);
                await SaveText(env, existing.NameKey, s.Value.Name, allLanguages: false);
                await SaveText(env, existing.AbbreviationKey, s.Value.Abbreviation, allLanguages: false);
                await RefreshCache(env);
                return Response.Html(AdminUnitViews.ViewRow(unit, env.Ctx) + AdminUnitViews.ErrorOob([], env.T));
            });
    };

    public static App Delete(UnitId id) => _ => async env =>
    {
        if ((await env.Units.Find(id)) is not [var unit])
        {
            return Response.Html(AdminUnitViews.ErrorOob([], env.T));
        }

        var recipes = await env.Recipes.All();
        if (recipes.Any(r => r.Ingredients.Any(i => i.Unit.Id.Value == id.Value)))
        {
            // Keep the row and surface the reason in the section's error banner.
            return Response.Html(AdminUnitViews.ViewRow(unit, env.Ctx) + AdminUnitViews.ErrorOob(["unit.error.in_use"], env.T));
        }

        await env.Units.Delete(id);
        return Response.Html(AdminUnitViews.ErrorOob([], env.T));
    };

    // New units get their text in every language so they resolve immediately; per-language
    // refinements happen later in the translation admin. Edits touch only the current language.
    private static async ValueTask SaveText(Env env, string key, string text, bool allLanguages)
    {
        if (env.Translations is null)
        {
            return;
        }

        if (allLanguages)
        {
            foreach (var lang in Languages.Supported)
            {
                await env.Translations.Save(key, lang, null, text);
            }
        }
        else
        {
            await env.Translations.Save(key, env.Language, null, text);
        }
    }

    private static async ValueTask RefreshCache(Env env)
    {
        if (env.TranslationCache is not null)
        {
            await env.TranslationCache.RefreshAsync();
        }
    }
}
