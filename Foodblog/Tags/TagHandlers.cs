namespace Foodblog.Tags;

public static class TagHandlers
{
    // Lists every recipe and article carrying the requested tag. The path segment is run through
    // Slug.From so that both a raw name and an already-slugified value resolve to the same tag.
    public static App Show(string rawSlug) => _ => async env =>
    {
        var slug = Slug.From(rawSlug);

        if (env.Tags is null || (await env.Tags.FindBySlug(slug)) is not [var tag])
        {
            return Response.NotFound(env.Ctx);
        }

        var recipes = await env.Recipes.FindByTag(slug);
        var articles = await env.Articles.FindByTag(slug);
        var users = await env.Users.All();
        var authorNames = users.ToDictionary(u => u.Id, u => u.DisplayName.Value);
        var authorAvatars = users.ToDictionary(u => u.Id, u => u.AvatarImageId);

        return Response.Html(TagViews.Show(tag, recipes, articles, authorNames, authorAvatars, env.Ctx));
    };
}
