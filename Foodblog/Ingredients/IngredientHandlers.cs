namespace Foodblog.Ingredients;

// Public, read-only ingredient pages (the editing counterpart lives in AdminIngredientHandlers).
public static class IngredientHandlers
{
    // Ingredient tiles per page on the public overview.
    private const int PageSize = 30;

    public static App Index => request => async env =>
    {
        var all = await env.Ingredients.All();
        var page = Pagination.Paginate(all, Pagination.RequestedPage(request), PageSize);
        return Response.Html(IngredientViews.Index(page, env.Ctx));
    };

    public static App Show(IngredientId id) => _ => async env =>
    {
        if ((await env.Ingredients.Find(id)) is not [var ingredient])
        {
            return Response.NotFound(env.Ctx);
        }

        return Response.Html(IngredientViews.Show(ingredient, env.Ctx));
    };
}
