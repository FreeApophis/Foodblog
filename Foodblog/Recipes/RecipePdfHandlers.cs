namespace Foodblog.Recipes;

// Streams a recipe as a generated A4 PDF (C1 default, C3 via ?design=alternative), offered as a
// download. Resolves the data the layout needs (author, ingredient names, cover image bytes), honours
// the ?portions=N serving count, and hands a plain model to RecipePdf.
public static class RecipePdfHandlers
{
    public static App Download(RecipeId id) => request => async env =>
    {
        if ((await env.Recipes.Find(id)) is not [var recipe])
        {
            return Response.NotFound(env.Ctx);
        }

        var t = env.T;
        var design = RecipePdfDesigns.Parse(request.Query.GetValueOrNone("design").GetOrElse(string.Empty));
        var displayPortions = RequestedPortions(request, recipe.Portions);

        var authorName = (await env.Users.FindById(recipe.AuthorId))
            .Select(user => user.DisplayName.Value)
            .GetOrElse("?");
        var ingredientNames = (await env.Ingredients.All()).ToDictionary(i => i.Id, i => i.Name.Value);
        var cover = await LoadCover(env, recipe);

        var model = RecipePdfMapper.Create(recipe, displayPortions, authorName, ingredientNames, cover.Bytes, t, cover.Focal) with { SiteName = env.SiteName };

        var bytes = RecipePdf.Generate(model, design, t);
        var filename = $"{Slug.From(recipe.Name.Value)}.pdf";

        return Response.Bytes("application/pdf", bytes, new Dictionary<string, string>
        {
            ["Content-Disposition"] = $"attachment; filename=\"{filename}\"",
        });
    };

    // Any positive ?portions=N scales the recipe; an absent or invalid value keeps the stored portions.
    private static int RequestedPortions(Request request, int fallback) =>
        request.Query.GetValueOrNone("portions")
            .Select(raw => int.TryParse(raw, out var n) && n >= 1 ? n : fallback)
            .GetOrElse(fallback);

    // The recipe's cover is the first image, stored as a "/images/{id}" URL — pull its bytes and crop
    // focal point for embedding. The focal point lets the PDF crop around the subject (as the web
    // slider does), not just the centre.
    private static async Task<(byte[]? Bytes, FocalPoint Focal)> LoadCover(Env env, Recipe recipe)
    {
        if (recipe.Images.Count == 0)
        {
            return (null, FocalPoint.Center);
        }

        var url = recipe.Images[0];
        var lastSlash = url.LastIndexOf('/');
        if (lastSlash < 0 || !int.TryParse(url[(lastSlash + 1)..], out var imageId))
        {
            return (null, FocalPoint.Center);
        }

        return (await env.Images.Find(new ImageId(imageId))) is [var image]
            ? (image.Data, image.Focal)
            : (null, FocalPoint.Center);
    }
}
