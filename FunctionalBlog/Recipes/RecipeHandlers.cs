using System.Globalization;

namespace FunctionalBlog.Recipes;

public static class RecipeHandlers
{
    // Default unit id for a freshly added ingredient row (Gramm).
    private const string DefaultUnitId = "1";

    public static App Index => request => async env =>
    {
        var recipes = await env.Recipes.All();
        var users = await env.Users.All();
        var authorNames = users.ToDictionary(u => u.Id, u => u.DisplayName.Value);
        var authorAvatars = users.ToDictionary(u => u.Id, u => u.AvatarImageId);

        var page = Pagination.Paginate(recipes, Pagination.RequestedPage(request));

        return Response.Html(RecipeViews.Index(page, authorNames, authorAvatars, env.Ctx));
    };

    public static App ShowRecipe(RecipeId id) => request => async env =>
        await (await env.Recipes.Find(id)).Match(
            none: () => Task.FromResult(Response.NotFound(env.Ctx)),
            some: async recipe =>
            {
                var author = await env.Users.FindById(recipe.AuthorId);
                var authorName = author.Select(u => u.DisplayName.Value).GetOrElse("?");
                var authorAvatar = author.SelectMany(u => u.AvatarImageId);
                var ingredients = (await env.Ingredients.All()).ToDictionary(i => i.Id);
                var displayPortions = RequestedPortions(request, recipe.Portions);
                return Response.Html(RecipeViews.Show(recipe, authorName, ingredients, displayPortions, env.Ctx, request.BaseUrl, authorAvatar));
            });

    // The detail page can be re-scaled to a different serving count via ?portions=N. Any positive
    // integer is accepted; an absent or invalid value falls back to the recipe's stored portions.
    private static int RequestedPortions(Request request, int fallback) =>
        request.Query.GetValueOrNone("portions")
            .Select(raw => int.TryParse(raw, out var n) && n >= 1 ? n : fallback)
            .GetOrElse(fallback);

    public static App NewRecipeForm => _ => async env =>
        Response.Html(RecipeViews.Form(
            errors: [],
            name: string.Empty,
            description: string.Empty,
            portions: "4",
            difficulty: "0",
            tags: string.Empty,
            hints: string.Empty,
            ingredients: [(string.Empty, string.Empty, DefaultUnitId)],
            steps: [string.Empty],
            prepTime: string.Empty,
            cookTime: string.Empty,
            ctx: env.Ctx,
            units: await env.Units.All()));

    public static App CreateRecipe => request => async env =>
    {
        var action = request.Form.GetValueOrDefault("action", string.Empty);
        var units = await env.Units.All();

        if (action == "add-ingredient" || action.StartsWith("remove-ingredient-"))
        {
            var formIngredients = RecipeForm.ParseIngredients(request);
            var formSteps = RecipeForm.ParseRawSteps(request);

            if (action == "add-ingredient")
            {
                formIngredients.Add((string.Empty, string.Empty, DefaultUnitId));
            }
            else if (int.TryParse(action["remove-ingredient-".Length..], out var removeIdx) && removeIdx < formIngredients.Count)
            {
                formIngredients.RemoveAt(removeIdx);
            }

            return Response.Html(RecipeViews.Form(
                [],
                ReadName(request),
                ReadDescription(request),
                ReadPortions(request),
                ReadDifficulty(request),
                ReadTags(request),
                ReadHints(request),
                formIngredients,
                formSteps,
                ReadPrepTime(request),
                ReadCookTime(request),
                env.Ctx,
                units));
        }

        if (action == "add-step" || action.StartsWith("remove-step-"))
        {
            var formIngredients = RecipeForm.ParseIngredients(request);
            var formSteps = RecipeForm.ParseRawSteps(request);

            if (action == "add-step")
            {
                formSteps.Add(string.Empty);
            }
            else if (int.TryParse(action["remove-step-".Length..], out var removeIdx) && removeIdx < formSteps.Count)
            {
                formSteps.RemoveAt(removeIdx);
            }

            return Response.Html(RecipeViews.Form(
                [],
                ReadName(request),
                ReadDescription(request),
                ReadPortions(request),
                ReadDifficulty(request),
                ReadTags(request),
                ReadHints(request),
                formIngredients,
                formSteps,
                ReadPrepTime(request),
                ReadCookTime(request),
                env.Ctx,
                units));
        }

        return await DecodeWithImages(request, units).Match(
            failure: f => Task.FromResult(Response.Html(
                RecipeViews.Form(
                    f.Error,
                    ReadName(request),
                    ReadDescription(request),
                    ReadPortions(request),
                    ReadDifficulty(request),
                    ReadTags(request),
                    ReadHints(request),
                    RecipeForm.ParseIngredients(request),
                    RecipeForm.ParseRawSteps(request),
                    ReadPrepTime(request),
                    ReadCookTime(request),
                    env.Ctx,
                    units,
                    existingImages: RecipeForm.ParseKeptImages(request)),
                400)),
            success: async s =>
            {
                var resolvedIngredients = await ResolveIngredients(env, s.Value.Form.Ingredients);
                var recipe = Recipe.Create(
                    id: await env.Recipes.NextId(),
                    name: s.Value.Form.Name,
                    description: s.Value.Form.Description,
                    preparationSteps: s.Value.Form.Steps,
                    authorId: ((AuthenticatedUser)env.CurrentUser).Id,
                    difficulty: s.Value.Form.Difficulty,
                    tags: s.Value.Form.Tags,
                    portions: s.Value.Form.Portions,
                    ingredients: resolvedIngredients,
                    images: await SaveImages(env, s.Value.Images),
                    hints: s.Value.Form.Hints,
                    preparationTime: s.Value.Form.PreparationTime,
                    cookingTime: s.Value.Form.CookingTime,
                    calorificValue: await CalculateCaloriesPerServing(env, resolvedIngredients, s.Value.Form.Portions));

                await env.Recipes.Save(recipe);
                env.Search?.IndexRecipe(recipe);
                var slug = await env.EnsureSlug(SlugEntityType.Recipe, recipe.Id.Value, recipe.Name.Value);
                await EnsureTagSlugs(env, recipe.Tags);
                return Response.Redirect($"/recipes/{slug}");
            });
    };

    public static App IngredientsSection => request => async env =>
    {
        var action = request.Form.GetValueOrDefault("action", string.Empty);
        var ingredients = RecipeForm.ParseIngredients(request);
        var autofocus = Option<int>.None;

        if (action == "add-ingredient")
        {
            ingredients.Add((string.Empty, string.Empty, DefaultUnitId));
            autofocus = Option.Some(ingredients.Count - 1);
        }
        else if (action.StartsWith("remove-ingredient-") &&
                 int.TryParse(action["remove-ingredient-".Length..], out var removeIdx) &&
                 removeIdx < ingredients.Count)
        {
            ingredients.RemoveAt(removeIdx);
        }

        return Response.Html(RecipeViews.IngredientSection(ingredients, env.T, await env.Units.All(), autofocus));
    };

    public static App StepsSection => request => env =>
    {
        var action = request.Form.GetValueOrDefault("action", string.Empty);
        var steps = RecipeForm.ParseRawSteps(request);
        var autofocus = Option<int>.None;

        if (action == "add-step")
        {
            steps.Add(string.Empty);
            autofocus = Option.Some(steps.Count - 1);
        }
        else if (action.StartsWith("remove-step-") &&
                 int.TryParse(action["remove-step-".Length..], out var removeIdx) &&
                 removeIdx < steps.Count)
        {
            steps.RemoveAt(removeIdx);
        }

        return ValueTask.FromResult(Response.Html(RecipeViews.StepSection(steps, env.T, autofocus)));
    };

    public static App IngredientSearch => request => async env =>
    {
        var index = request.Form.GetValueOrDefault("index", "0");
        var query = request.Form.GetValueOrDefault($"ingredient_name_{index}", string.Empty).Trim();

        var matches = query.Length == 0
            ? []
            : (await env.Ingredients.All())
                .Where(i => i.Name.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

        return Response.Html(RecipeViews.IngredientMatches(index, query, matches, env.T));
    };

    public static App IngredientSelect => request => env =>
    {
        var index = request.Form.GetValueOrDefault("index", "0");
        var name = request.Form.GetValueOrDefault("selected", string.Empty);
        return ValueTask.FromResult(Response.Html(RecipeViews.IngredientCombobox(index, name, env.T)));
    };

    public static App DeleteRecipe(RecipeId id) => _ => async env =>
    {
        if ((await env.Recipes.Find(id)) == Option<Recipe>.None)
        {
            return Response.NotFound(env.Ctx);
        }

        await env.Recipes.Delete(id);
        env.Search?.DeleteDocument("recipe", id.Value);
        return Response.Redirect("/recipes");
    };

    public static App EditRecipeForm(RecipeId id) => _ => async env =>
        await (await env.Recipes.Find(id)).Match(
            none: () => Task.FromResult(Response.NotFound(env.Ctx)),
            some: async recipe =>
            {
                var availableIngredients = await env.Ingredients.All();
                var nameById = availableIngredients.ToDictionary(i => i.Id, i => i.Name.Value);

                var ingredients = recipe.Ingredients
                    .Select(ri => (
                        nameById.GetValueOrNone(ri.IngredientId).GetOrElse(string.Empty),
                        ri.Amount.ToString(CultureInfo.InvariantCulture),
                        ri.Unit.Id.Value.ToString()))
                    .ToList<(string Name, string Amount, string Unit)>();

                var steps = recipe.PreparationSteps
                    .OrderBy(s => s.SortOrder)
                    .Select(s => s.Text)
                    .ToList();

                return Response.Html(RecipeViews.Form(
                    errors: [],
                    name: recipe.Name.Value,
                    description: recipe.Description.Value,
                    portions: recipe.Portions.ToString(),
                    difficulty: ((int)recipe.Difficulty).ToString(),
                    tags: string.Join(", ", recipe.Tags.Select(tag => tag.Value)),
                    hints: string.Join("\n", recipe.Hints.Select(h => h.Text)),
                    ingredients: ingredients,
                    steps: steps,
                    prepTime: recipe.PreparationTime.ToString(),
                    cookTime: recipe.CookingTime.ToString(),
                    ctx: env.Ctx,
                    units: await env.Units.All(),
                    formAction: $"/recipes/{id.Value}",
                    titleKey: "recipe.edit_title",
                    existingImages: recipe.Images));
            });

    public static App UpdateRecipe(RecipeId id) => request => async env =>
    {
        if ((await env.Recipes.Find(id)) is not [var existing])
        {
            return Response.NotFound(env.Ctx);
        }

        var action = request.Form.GetValueOrDefault("action", string.Empty);
        var formAction = $"/recipes/{id.Value}";
        var units = await env.Units.All();

        if (action == "add-ingredient" || action.StartsWith("remove-ingredient-"))
        {
            var formIngredients = RecipeForm.ParseIngredients(request);
            var formSteps = RecipeForm.ParseRawSteps(request);

            if (action == "add-ingredient")
            {
                formIngredients.Add((string.Empty, string.Empty, DefaultUnitId));
            }
            else if (int.TryParse(action["remove-ingredient-".Length..], out var removeIdx) && removeIdx < formIngredients.Count)
            {
                formIngredients.RemoveAt(removeIdx);
            }

            return Response.Html(RecipeViews.Form(
                [],
                ReadName(request),
                ReadDescription(request),
                ReadPortions(request),
                ReadDifficulty(request),
                ReadTags(request),
                ReadHints(request),
                formIngredients,
                formSteps,
                ReadPrepTime(request),
                ReadCookTime(request),
                env.Ctx,
                units,
                formAction,
                "recipe.edit_title",
                RecipeForm.ParseKeptImages(request)));
        }

        if (action == "add-step" || action.StartsWith("remove-step-"))
        {
            var formIngredients = RecipeForm.ParseIngredients(request);
            var formSteps = RecipeForm.ParseRawSteps(request);

            if (action == "add-step")
            {
                formSteps.Add(string.Empty);
            }
            else if (int.TryParse(action["remove-step-".Length..], out var removeIdx) && removeIdx < formSteps.Count)
            {
                formSteps.RemoveAt(removeIdx);
            }

            return Response.Html(RecipeViews.Form(
                [],
                ReadName(request),
                ReadDescription(request),
                ReadPortions(request),
                ReadDifficulty(request),
                ReadTags(request),
                ReadHints(request),
                formIngredients,
                formSteps,
                ReadPrepTime(request),
                ReadCookTime(request),
                env.Ctx,
                units,
                formAction,
                "recipe.edit_title",
                RecipeForm.ParseKeptImages(request)));
        }

        return await DecodeWithImages(request, units).Match(
            failure: f => Task.FromResult(Response.Html(
                RecipeViews.Form(
                    f.Error,
                    ReadName(request),
                    ReadDescription(request),
                    ReadPortions(request),
                    ReadDifficulty(request),
                    ReadTags(request),
                    ReadHints(request),
                    RecipeForm.ParseIngredients(request),
                    RecipeForm.ParseRawSteps(request),
                    ReadPrepTime(request),
                    ReadCookTime(request),
                    env.Ctx,
                    units,
                    formAction,
                    "recipe.edit_title",
                    RecipeForm.ParseKeptImages(request)),
                400)),
            success: async s =>
            {
                var keptImages = RecipeForm.ParseKeptImages(request);
                var newImages = await SaveImages(env, s.Value.Images);
                var resolvedIngredients = await ResolveIngredients(env, s.Value.Form.Ingredients);

                var updated = Recipe.Create(
                    id: id,
                    name: s.Value.Form.Name,
                    description: s.Value.Form.Description,
                    preparationSteps: s.Value.Form.Steps,
                    authorId: existing.AuthorId,
                    difficulty: s.Value.Form.Difficulty,
                    tags: s.Value.Form.Tags,
                    portions: s.Value.Form.Portions,
                    ingredients: resolvedIngredients,
                    images: [.. keptImages, .. newImages],
                    hints: s.Value.Form.Hints,
                    preparationTime: s.Value.Form.PreparationTime,
                    cookingTime: s.Value.Form.CookingTime,
                    calorificValue: await CalculateCaloriesPerServing(env, resolvedIngredients, s.Value.Form.Portions));

                await env.Recipes.Save(updated);
                env.Search?.IndexRecipe(updated);
                var slug = await env.EnsureSlug(SlugEntityType.Recipe, id.Value, updated.Name.Value);
                await EnsureTagSlugs(env, updated.Tags);
                return Response.Redirect($"/recipes/{slug}");
            });
    };

    // Registers a slug for each of the recipe's tags so /tag/{slug} links resolve immediately after a
    // save (the tag rows were just upserted by the repository). No-op when there is no tag store.
    private static async Task EnsureTagSlugs(Env env, IReadOnlyList<RecipeTag> tags)
    {
        if (env.Tags is not { } tagRepo)
        {
            return;
        }

        foreach (var name in tags
            .Select(tag => tag.Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await tagRepo.FindIdByName(name) is [var id])
            {
                await env.EnsureSlug(SlugEntityType.Tag, id, name);
            }
        }
    }

    private sealed record RecipeWithImages(RecipeForm.Valid Form, IReadOnlyList<ImageUploadForm.Valid> Images);

    private static Validated<IReadOnlyList<string>, RecipeWithImages> DecodeWithImages(Request request, IReadOnlyList<Unit> units)
    {
        Func<RecipeForm.Valid, IReadOnlyList<ImageUploadForm.Valid>, RecipeWithImages> combine =
            (form, images) => new RecipeWithImages(form, images);

        return combine
            .Apply(RecipeForm.Decode(request, units), CombineErrors)
            .Apply(ImageUploadForm.DecodeMany(request, "images"), CombineErrors);
    }

    // Resolves each typed ingredient line to an IngredientId, quick-creating a new ingredient
    // (with default nutrition) whenever the name doesn't match an existing one. Runs in the
    // handler because it needs the repository — the form decoder stays pure.
    private static async Task<IReadOnlyList<RecipeIngredient>> ResolveIngredients(
        Env env, IReadOnlyList<RecipeForm.IngredientLine> lines)
    {
        // Imported data can hold several ingredients with the same name (e.g. two "Apfel"),
        // so build the lookup tolerantly — first occurrence wins — rather than ToDictionary,
        // which would throw on a duplicate key.
        var byName = new Dictionary<string, IngredientId>(StringComparer.OrdinalIgnoreCase);
        foreach (var ing in await env.Ingredients.All())
        {
            if (!byName.ContainsKey(ing.Name.Value))
            {
                byName[ing.Name.Value] = ing.Id;
            }
        }

        var result = new List<RecipeIngredient>();
        foreach (var line in lines)
        {
            IngredientId id;
            if (byName.GetValueOrNone(line.Name) is [var existing])
            {
                id = existing;
            }
            else
            {
                var created = Ingredient.Create(
                    id: await env.Ingredients.NextId(),
                    name: new IngredientName(line.Name),
                    image: string.Empty,
                    description: string.Empty,
                    density: 1,
                    pieceCount: 0,
                    calorificValue: 0,
                    protein: 0,
                    fat: 0,
                    carbohydrates: 0,
                    sugar: 0,
                    fiber: 0);
                await env.Ingredients.Save(created);
                env.Search?.IndexIngredient(created);
                await env.EnsureSlug(SlugEntityType.Ingredient, created.Id.Value, created.Name.Value);
                byName[line.Name] = created.Id;
                id = created.Id;
            }

            result.Add(new RecipeIngredient(id, line.Amount, line.Unit));
        }

        return result;
    }

    private static async Task<IReadOnlyList<string>> SaveImages(Env env, IReadOnlyList<ImageUploadForm.Valid> uploads)
    {
        var urls = new List<string>();
        foreach (var upload in uploads)
        {
            var image = Image.Create(
                id: await env.Images.NextId(),
                fileName: upload.FileName,
                contentType: upload.ContentType,
                data: upload.Content,
                uploadedBy: ((AuthenticatedUser)env.CurrentUser).Id,
                createdAt: env.Clock.Now);

            await env.Images.Save(image);
            urls.Add($"/images/{image.Id.Value}");
        }

        return urls;
    }

    private static IReadOnlyList<string> CombineErrors(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];

    private static string ReadName(Request r) => r.Form.GetValueOrDefault("name", string.Empty);

    private static string ReadDescription(Request r) => r.Form.GetValueOrDefault("description", string.Empty);

    private static string ReadPortions(Request r) => r.Form.GetValueOrDefault("portions", "4");

    private static string ReadDifficulty(Request r) => r.Form.GetValueOrDefault("difficulty", "0");

    private static string ReadTags(Request r) => r.Form.GetValueOrDefault("tags", string.Empty);

    private static string ReadHints(Request r) => r.Form.GetValueOrDefault("hints", string.Empty);

    private static string ReadPrepTime(Request r) => r.Form.GetValueOrDefault("preparation_time", string.Empty);

    private static string ReadCookTime(Request r) => r.Form.GetValueOrDefault("cooking_time", string.Empty);

    // The recipe's calorie figure is derived from its ingredients (not entered): the whole meal's
    // calories divided by the serving count. Quick-created ingredients start at 0 kcal and simply
    // contribute nothing until their nutrition is filled in and the recipe is saved again.
    private static async Task<int> CalculateCaloriesPerServing(Env env, IReadOnlyList<RecipeIngredient> ingredients, int portions)
    {
        var catalog = (await env.Ingredients.All()).ToDictionary(i => i.Id);
        return CalorieCalculator.PerServing(ingredients, portions, catalog);
    }
}
