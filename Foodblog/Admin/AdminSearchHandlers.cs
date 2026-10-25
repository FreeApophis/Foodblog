namespace Foodblog.Admin;

public static class AdminSearchHandlers
{
    // The /admin/search maintenance page. Gated by RequirePermission<Manage>(SearchResource).
    public static App Status => _ => env =>
        ValueTask.FromResult(Response.Html(AdminSearchViews.Status(ReadStatus(env), env.Ctx)));

    // Rebuilds the full-text index from the database (the source of truth), then returns the
    // refreshed status panel (htmx swaps it in place). Inline is fine: a rebuild is a single commit
    // over a small dataset (see the index notes), and htmx keeps the page responsive.
    public static App Rebuild => _ => async env =>
    {
        if (env.Search is { } index)
        {
            await index.RebuildAsync(env.Articles, env.Recipes, env.Ingredients, env.Pages);
        }

        return Response.Html(AdminSearchViews.Panel(ReadStatus(env), rebuilt: true, env.Ctx));
    };

    private static SearchIndexStatus ReadStatus(Env env) =>
        env.Search?.Status() ?? new SearchIndexStatus(Option<DateTimeOffset>.None, 0, 0, 0, 0);
}
