using System.Globalization;

namespace Foodblog.Ingredients;

public static class AdminIngredientHandlers
{
    // Ingredient rows per page on the overview.
    private const int PageSize = 15;

    public static App List => request => async env =>
    {
        var all = await env.Ingredients.All();
        var page = Pagination.Paginate(all, Pagination.RequestedPage(request), PageSize);
        var error = request.Query.GetValueOrDefault("error", string.Empty);
        return Response.Html(AdminIngredientViews.List(page, env.Ctx, error));
    };

    public static App NewForm
        => _ => env => ValueTask.FromResult(Response.Html(AdminIngredientViews.Form(
            errors: [],
            name: string.Empty,
            description: string.Empty,
            image: string.Empty,
            density: "1",
            pieceCount: "0",
            calorificValue: "0",
            protein: "0",
            fat: "0",
            carbohydrates: "0",
            sugar: "0",
            fiber: "0",
            ctx: env.Ctx)));

    public static App Create
        => request => async env => await IngredientForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(RenderForm(request, f.Error, env.Ctx), 400)),
            success: async s =>
            {
                var ingredient = BuildIngredient(await env.Ingredients.NextId(), s.Value);
                await env.Ingredients.Save(ingredient);
                env.Search?.IndexIngredient(ingredient);
                await env.EnsureSlug(SlugEntityType.Ingredient, ingredient.Id.Value, ingredient.Name.Value);
                return Response.Redirect("/admin/ingredients");
            });

    public static App EditForm(IngredientId id) => _ => async env =>
    {
        if ((await env.Ingredients.Find(id)) is not [var ingredient])
        {
            return Response.NotFound(env.Ctx);
        }

        return Response.Html(AdminIngredientViews.Form(
            errors: [],
            name: ingredient.Name.Value,
            description: ingredient.Description,
            image: ingredient.Image,
            density: ingredient.Density.ToString(CultureInfo.InvariantCulture),
            pieceCount: ingredient.PieceCount.ToString(CultureInfo.InvariantCulture),
            calorificValue: ingredient.CalorificValue.ToString(CultureInfo.InvariantCulture),
            protein: ingredient.Protein.ToString(CultureInfo.InvariantCulture),
            fat: ingredient.Fat.ToString(CultureInfo.InvariantCulture),
            carbohydrates: ingredient.Carbohydrates.ToString(CultureInfo.InvariantCulture),
            sugar: ingredient.Sugar.ToString(CultureInfo.InvariantCulture),
            fiber: ingredient.Fiber.ToString(CultureInfo.InvariantCulture),
            ctx: env.Ctx,
            formAction: $"/admin/ingredients/{id.Value}",
            titleKey: "ingredient.edit_title"));
    };

    public static App Update(IngredientId id) => request => async env =>
    {
        if ((await env.Ingredients.Find(id)) is not [_])
        {
            return Response.NotFound(env.Ctx);
        }

        return await IngredientForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(
                RenderForm(request, f.Error, env.Ctx, $"/admin/ingredients/{id.Value}", "ingredient.edit_title"),
                400)),
            success: async s =>
            {
                var updated = BuildIngredient(id, s.Value);
                await env.Ingredients.Save(updated);
                env.Search?.IndexIngredient(updated);
                await env.EnsureSlug(SlugEntityType.Ingredient, id.Value, updated.Name.Value);
                return Response.Redirect("/admin/ingredients");
            });
    };

    public static App Delete(IngredientId id) => _ => async env =>
    {
        var recipes = await env.Recipes.All();
        if (recipes.Any(r => r.Ingredients.Any(i => i.IngredientId.Value == id.Value)))
        {
            return Response.Redirect("/admin/ingredients?error=in-use");
        }

        if ((await env.Ingredients.Find(id)) is [_])
        {
            await env.Ingredients.Delete(id);
            env.Search?.DeleteDocument("ingredient", id.Value);
            return Response.Redirect("/admin/ingredients");
        }

        return Response.NotFound(env.Ctx);
    };

    // Re-render the form with the submitted values so a validation failure keeps the user's input.
    private static string RenderForm(
        Request request,
        IReadOnlyList<string> errors,
        ViewContext ctx,
        string formAction = "/admin/ingredients",
        string titleKey = "ingredient.new_title") =>
        AdminIngredientViews.Form(
            errors,
            Read(request, "name"),
            Read(request, "description"),
            Read(request, "image"),
            Read(request, "density"),
            Read(request, "piece_count"),
            Read(request, "calorific_value"),
            Read(request, "protein"),
            Read(request, "fat"),
            Read(request, "carbohydrates"),
            Read(request, "sugar"),
            Read(request, "fiber"),
            ctx,
            formAction,
            titleKey);

    private static string Read(Request request, string key) =>
        request.Form.GetValueOrNone(key).GetOrElse(string.Empty);

    private static Ingredient BuildIngredient(IngredientId id, IngredientForm.Valid v) =>
        Ingredient.Create(
            id,
            v.Name,
            v.Image,
            v.Description,
            v.Density,
            v.PieceCount,
            v.CalorificValue,
            v.Protein,
            v.Fat,
            v.Carbohydrates,
            v.Sugar,
            v.Fiber);
}
