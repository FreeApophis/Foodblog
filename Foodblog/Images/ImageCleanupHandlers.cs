namespace Foodblog.Images;

public static class ImageCleanupHandlers
{
    // The /admin/images/cleanup page: lists library images that nothing references.
    public static App Cleanup => _ => async env =>
        Response.Html(ImageCleanupViews.Cleanup(await LoadOrphans(env), env.Ctx));

    // Removes every currently-unreferenced image. Deleting an orphan cannot orphan another image
    // (it removes no references), so a single pass over the current orphan set is complete.
    public static App DeleteUnused => _ => async env =>
    {
        foreach (var orphan in await LoadOrphans(env))
        {
            await env.Images.Delete(orphan.Id);
        }

        return Response.Html(ImageCleanupViews.Cleanup(await LoadOrphans(env), env.Ctx, deleted: true));
    };

    private static async Task<IReadOnlyList<ImageSummary>> LoadOrphans(Env env)
    {
        var referenced = ImageUsage.ReferencedIds(
            await env.Articles.All(),
            await env.Pages.All(),
            await env.Recipes.All(),
            await env.Ingredients.All(),
            await env.Users.All());

        return ImageUsage.Orphans(await env.Images.List(), referenced);
    }
}
