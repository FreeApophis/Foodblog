namespace FunctionalBlog.Admin;

public static class AdminDashboardHandlers
{
    // The /admin landing page. Reachable by any authenticated user (the route gates with
    // RequireAuth); the view only renders the stats and cards the principal has access to.
    public static App Dashboard => _ => async env =>
    {
        var ingredients = await env.Ingredients.All();

        var stats = new DashboardStats(
            Articles: (await env.Articles.All()).Count,
            Recipes: (await env.Recipes.All()).Count,
            Ingredients: ingredients.Count,
            Pages: (await env.Pages.All()).Count,
            Images: (await env.Images.List()).Count,
            Users: (await env.Users.All()).Count,
            IncompleteIngredients: ingredients.Count(i => i.HasMissingInformation),
            SiteUrlMissing: Option.FromNullable(env.Config).SelectMany(config => config.PublicBaseUrl) is []);

        return Response.Html(AdminDashboardViews.Dashboard(stats, env.Ctx));
    };
}
