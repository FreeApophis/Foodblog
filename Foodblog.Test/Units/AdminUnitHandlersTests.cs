namespace Foodblog.Test.Units;

public sealed class AdminUnitHandlersTests
{
    [Fact]
    public async Task List_renders_inline_rows_and_an_add_button()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.List(AnEmptyRequest())(env);

        Assert.Contains("units-section", response.Body);
        Assert.Contains("hx-get=\"/admin/units/new-row\"", response.Body);
        Assert.Contains("/admin/units/1/edit-row", response.Body);
        Assert.Contains("/admin/units/1/delete", response.Body);
    }

    [Fact]
    public async Task List_renders_an_admin_breadcrumb_instead_of_a_back_link()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.List(AnEmptyRequest())(env);

        Assert.Contains("class=\"breadcrumb\"", response.Body);
        Assert.Contains("href=\"/admin\"", response.Body);
        Assert.DoesNotContain("common.back_to_admin", response.Body);
    }

    [Fact]
    public async Task Create_persists_a_new_unit_with_category_and_factor()
    {
        var env = await BuildEnv();
        var request = ARequest(Form("Tasse", "Tasse", "1", "0.25"));

        var response = await AdminUnitHandlers.Create(request)(env);

        Assert.Equal(200, response.Status);
        var created = Assert.Single(await env.Units.All(), u => u.Factor == 0.25m);
        Assert.Equal(UnitCategory.Volume, created.Category);
        Assert.Equal($"unit.{created.Id.Value}.name", created.NameKey);
        Assert.Contains("Tasse", response.Body);
    }

    [Fact]
    public async Task Create_saves_the_translated_name_and_abbreviation()
    {
        var env = await BuildEnv();

        await AdminUnitHandlers.Create(ARequest(Form("Tasse", "Tasse", "1", "0.25")))(env);

        var created = Assert.Single(await env.Units.All(), u => u.Factor == 0.25m);
        var translations = await env.Translations!.All();
        Assert.Contains(translations, t => t.Key == created.NameKey && t.Text == "Tasse");
        Assert.Contains(translations, t => t.Key == created.AbbreviationKey && t.Text == "Tasse");
    }

    [Fact]
    public async Task Create_with_invalid_input_re_renders_the_add_row_with_errors()
    {
        var env = await BuildEnv();
        var before = (await env.Units.All()).Count;

        var response = await AdminUnitHandlers.Create(ARequest(Form(string.Empty, "EL", "1", "1")))(env);

        Assert.Equal(400, response.Status);
        Assert.Contains("unit-row-new", response.Body);
        Assert.Contains("unit.error.name_required", response.Body);
        Assert.Equal(before, (await env.Units.All()).Count);
    }

    [Fact]
    public async Task Update_changes_the_factor_and_name()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.Update(new UnitId(2))(ARequest(Form("Kilo", "kg", "0", "2")))(env);

        Assert.Equal(200, response.Status);
        var unit = Assert.Single(await env.Units.All(), u => u.Id.Value == 2);
        Assert.Equal(2m, unit.Factor);
        Assert.Contains("Kilo", response.Body);
    }

    [Fact]
    public async Task EditRow_returns_an_inline_editor_for_the_unit()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.EditRow(new UnitId(1))(AnEmptyRequest())(env);

        Assert.Contains("unit-row-1", response.Body);
        Assert.Contains("hx-post=\"/admin/units/1\"", response.Body);
        Assert.Contains("name=\"factor\"", response.Body);
    }

    [Fact]
    public async Task NewRow_returns_an_empty_inline_editor_that_posts_to_create()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.NewRow(AnEmptyRequest())(env);

        Assert.Contains("unit-row-new", response.Body);
        Assert.Contains("hx-post=\"/admin/units\"", response.Body);
    }

    [Fact]
    public async Task Row_returns_the_read_only_row()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.Row(new UnitId(1))(AnEmptyRequest())(env);

        Assert.Contains("unit-row-1", response.Body);
        Assert.Contains("/admin/units/1/edit-row", response.Body);
    }

    [Fact]
    public async Task Delete_removes_a_unit_that_is_not_used()
    {
        var env = await BuildEnv();

        var response = await AdminUnitHandlers.Delete(new UnitId(2))(AnEmptyRequest())(env);

        Assert.Equal(200, response.Status);
        FunctionalAssert.None(await env.Units.Find(new UnitId(2)));
    }

    [Fact]
    public async Task Delete_is_blocked_when_the_unit_is_used_by_a_recipe()
    {
        var env = await BuildEnv();
        await SeedRecipeUsing(env, Gram);

        var response = await AdminUnitHandlers.Delete(Gram.Id)(AnEmptyRequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("unit.error.in_use", response.Body);
        Assert.True((await env.Units.Find(Gram.Id)) is [_]);
    }

    private static async Task SeedRecipeUsing(Env env, Unit unit)
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
            [new RecipeIngredient(new IngredientId(1), 200m, unit)],
            [],
            []));
    }

    private static async Task<Env> BuildEnv()
    {
        var translations = new InMemoryTranslationRepository();
        var env = new Env(
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
            Pages: new InMemoryPageRepository(),
            Translations: translations);

        return env with { TranslationCache = await TranslationCache.LoadAsync(translations) };
    }

    private static Request ARequest(IReadOnlyDictionary<string, string> form) =>
        new(HttpMethod.Post, "/admin/units", Empty, Empty, form, Empty);

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/admin/units", Empty, Empty, Empty, Empty);

    private static Dictionary<string, string> Form(string name, string abbreviation, string category, string factor) =>
        new()
        {
            ["name"] = name,
            ["abbreviation"] = abbreviation,
            ["category"] = category,
            ["factor"] = factor,
        };

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
