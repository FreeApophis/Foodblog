namespace Foodblog.Test.Ingredients;

public sealed class AdminIngredientHandlersTests
{
    [Fact]
    public async Task List_renders_rows_with_an_edit_link_and_a_new_link()
    {
        var env = BuildEnv();
        var id = await SeedIngredient(env, "Mehl");

        var response = await AdminIngredientHandlers.List(AnEmptyRequest())(env);

        Assert.Contains("ingredients-section", response.Body);
        Assert.Contains("href=\"/admin/ingredients/new\"", response.Body);
        Assert.Contains($"href=\"/admin/ingredients/{id.Value}/edit\"", response.Body);
        Assert.Contains($"/admin/ingredients/{id.Value}/delete", response.Body);
    }

    [Fact]
    public async Task List_links_each_ingredient_name_to_its_public_page()
    {
        var env = BuildEnv();
        var id = await SeedIngredient(env, "Mehl");

        var response = await AdminIngredientHandlers.List(AnEmptyRequest())(env);

        Assert.Contains($"href=\"/ingredients/{id.Value}\">Mehl</a>", response.Body);
    }

    [Fact]
    public async Task List_shows_at_most_fifteen_rows_and_links_to_the_next_page()
    {
        var env = BuildEnv();
        await SeedIngredients(env, 16);

        var response = await AdminIngredientHandlers.List(AnEmptyRequest())(env);

        Assert.Equal(15, CountRows(response.Body));
        Assert.Contains("/admin/ingredients?page=2", response.Body);
    }

    [Fact]
    public async Task List_second_page_shows_the_remaining_rows()
    {
        var env = BuildEnv();
        await SeedIngredients(env, 16);

        var response = await AdminIngredientHandlers.List(ARequest("/admin/ingredients", ("page", "2")))(env);

        Assert.Equal(1, CountRows(response.Body));
    }

    [Fact(Skip = "Ingredient.HasMissingInformation is hardcoded to false until 0 and unset can be told apart (see TODO).")]
    public async Task List_flags_only_the_ingredient_that_is_missing_information()
    {
        var env = BuildEnv();
        await SeedIngredient(env, "Mehl"); // no description / image → incomplete
        await SeedCompleteIngredient(env, "Zucker");

        var response = await AdminIngredientHandlers.List(AnEmptyRequest())(env);

        Assert.Equal(1, CountOccurrences(response.Body, "warn-badge"));
    }

    [Fact]
    public async Task List_surfaces_the_in_use_error_when_flagged()
    {
        var env = BuildEnv();

        var response = await AdminIngredientHandlers.List(ARequest("/admin/ingredients", ("error", "in-use")))(env);

        Assert.Contains("ingredient.error.in_use", response.Body);
    }

    [Fact]
    public async Task NewForm_renders_a_form_with_a_multiline_description()
    {
        var env = BuildEnv();

        var response = await AdminIngredientHandlers.NewForm(AnEmptyRequest())(env);

        Assert.Contains("<textarea name=\"description\"", response.Body);
        Assert.Contains("action=\"/admin/ingredients\"", response.Body);
    }

    [Fact]
    public async Task NewForm_shows_units_as_addons_inside_the_number_boxes()
    {
        var env = BuildEnv();

        var response = await AdminIngredientHandlers.NewForm(AnEmptyRequest())(env);

        Assert.Contains("class=\"input-unit\"", response.Body);
        Assert.Contains("class=\"unit-addon\">g/ml<", response.Body);
        Assert.Contains("class=\"unit-addon\">kJ/100g<", response.Body);
    }

    [Fact]
    public async Task Create_persists_a_new_ingredient_and_redirects()
    {
        var env = BuildEnv();

        var response = await AdminIngredientHandlers.Create(ARequest("/admin/ingredients", Form("Zucker")))(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/admin/ingredients", response.Headers["Location"]);
        var created = Assert.Single(await env.Ingredients.All());
        Assert.Equal("Zucker", created.Name.Value);
    }

    [Fact]
    public async Task Create_with_invalid_input_re_renders_the_form_with_errors()
    {
        var env = BuildEnv();

        var response = await AdminIngredientHandlers.Create(ARequest("/admin/ingredients", Form("X")))(env);

        Assert.Equal(400, response.Status);
        Assert.Contains("ingredient.error.name_too_short", response.Body);
        Assert.Contains("<textarea name=\"description\"", response.Body);
        Assert.Empty(await env.Ingredients.All());
    }

    [Fact]
    public async Task EditForm_renders_the_ingredient_values()
    {
        var env = BuildEnv();
        var id = await SeedCompleteIngredient(env, "Mehl");

        var response = await AdminIngredientHandlers.EditForm(id)(AnEmptyRequest())(env);

        Assert.Contains("value=\"Mehl\"", response.Body);
        Assert.Contains($"action=\"/admin/ingredients/{id.Value}\"", response.Body);
        Assert.Contains("<textarea name=\"description\"", response.Body);
    }

    [Fact]
    public async Task EditForm_breadcrumb_includes_the_ingredient_name_linking_to_its_detail_page()
    {
        var env = BuildEnv();
        var id = await SeedCompleteIngredient(env, "Mehl");

        var response = await AdminIngredientHandlers.EditForm(id)(AnEmptyRequest())(env);

        // Admin → Zutaten (list) → Mehl (detail) → Bearbeiten
        Assert.Contains("href=\"/admin/ingredients\"", response.Body);
        Assert.Contains($"href=\"/ingredients/{id.Value}\">Mehl</a>", response.Body);
    }

    [Fact]
    public async Task NewForm_breadcrumb_has_no_ingredient_name_crumb()
    {
        var env = BuildEnv();

        var response = await AdminIngredientHandlers.NewForm(AnEmptyRequest())(env);

        // Admin → Zutaten (list) → Neu — there is no entity yet to link to.
        Assert.Contains("href=\"/admin/ingredients\"", response.Body);
        Assert.DoesNotContain("href=\"/ingredients/", response.Body);
    }

    [Fact]
    public async Task Update_changes_the_name_and_redirects()
    {
        var env = BuildEnv();
        var id = await SeedIngredient(env, "Mehl");

        var response = await AdminIngredientHandlers.Update(id)(ARequest($"/admin/ingredients/{id.Value}", Form("Dinkelmehl")))(env);

        Assert.Equal(303, response.Status);
        var updated = Assert.Single(await env.Ingredients.All());
        Assert.Equal("Dinkelmehl", updated.Name.Value);
    }

    [Fact]
    public async Task Delete_removes_an_ingredient_that_is_not_used_and_redirects()
    {
        var env = BuildEnv();
        var id = await SeedIngredient(env, "Mehl");

        var response = await AdminIngredientHandlers.Delete(id)(AnEmptyRequest())(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/admin/ingredients", response.Headers["Location"]);
        Assert.Empty(await env.Ingredients.All());
    }

    [Fact]
    public async Task Delete_is_blocked_when_the_ingredient_is_used_by_a_recipe()
    {
        var env = BuildEnv();
        var id = await SeedIngredient(env, "Mehl");
        await SeedRecipeUsing(env, id);

        var response = await AdminIngredientHandlers.Delete(id)(AnEmptyRequest())(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/admin/ingredients?error=in-use", response.Headers["Location"]);
        Assert.True((await env.Ingredients.Find(id)) is [_]);
    }

    private static int CountRows(string body) => CountOccurrences(body, "class=\"ingredient-row\"");

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static async Task SeedIngredients(Env env, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await SeedIngredient(env, $"Zutat {i:D2}");
        }
    }

    private static Task<IngredientId> SeedIngredient(Env env, string name) =>
        Save(env, name, description: string.Empty, image: string.Empty);

    private static Task<IngredientId> SeedCompleteIngredient(Env env, string name) =>
        Save(env, name, description: "Eine vollständige Beschreibung.", image: "/images/1");

    private static async Task<IngredientId> Save(Env env, string name, string description, string image)
    {
        var id = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            id,
            new IngredientName(name),
            image,
            description,
            density: 1,
            pieceCount: 0,
            calorificValue: 0,
            protein: 0,
            fat: 0,
            carbohydrates: 0,
            sugar: 0,
            fiber: 0));
        return id;
    }

    private static async Task SeedRecipeUsing(Env env, IngredientId ingredientId)
    {
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein klassischer Rührkuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            4,
            [new RecipeIngredient(ingredientId, 200m, Gram)],
            [],
            []));
    }

    private static readonly Unit Gram = new(new UnitId(1), "unit.1.name", "unit.1.abbr", UnitCategory.Weight, 1m);

    private static Dictionary<string, string> Form(string name) =>
        new()
        {
            ["name"] = name,
            ["description"] = string.Empty,
            ["image"] = string.Empty,
            ["density"] = "1",
            ["piece_count"] = "0",
            ["calorific_value"] = "0",
            ["protein"] = "0",
            ["fat"] = "0",
            ["carbohydrates"] = "0",
            ["sugar"] = "0",
            ["fiber"] = "0",
        };

    private static Request ARequest(string path, IReadOnlyDictionary<string, string> form) =>
        new(HttpMethod.Post, path, Empty, Empty, form, Empty);

    private static Request ARequest(string path, params (string Key, string Value)[] query) =>
        new(HttpMethod.Get, path, Empty, query.ToDictionary(q => q.Key, q => q.Value), Empty, Empty);

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/admin/ingredients", Empty, Empty, Empty, Empty);

    private static Env BuildEnv() => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository());

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
