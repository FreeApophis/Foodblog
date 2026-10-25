namespace Foodblog.Identity;

public static class UsersHandlers
{
    // Public-facing (login-gated) community directory: every user with their recipe count,
    // contributors first. Distinct from the permission-gated /admin/users management page.
    public static App Index => _ => async env =>
    {
        var users = await env.Users.All();
        var recipes = await env.Recipes.All();

        var countByAuthor = recipes
            .GroupBy(r => r.AuthorId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var entries = users
            .Select(u => (User: u, RecipeCount: countByAuthor.GetValueOrDefault(u.Id.Value, 0)))
            .OrderByDescending(e => e.RecipeCount)
            .ThenByDescending(e => e.User.CreatedAt)
            .ThenBy(e => e.User.DisplayName.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Response.Html(UsersViews.Index(entries, env.Ctx));
    };
}
