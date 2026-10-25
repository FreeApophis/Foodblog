namespace Foodblog.Test.Ingredients;

public sealed class IngredientHandlersTests
{
    [Fact]
    public async Task Show_returns_404_for_an_unknown_id()
    {
        var env = BuildEnv();

        var response = await IngredientHandlers.Show(new IngredientId(404))(AnEmptyRequest())(env);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Show_renders_the_name_description_and_nutrition()
    {
        var env = BuildEnv();
        var id = await SeedComplete(env, "Mehl", "Feines Weizenmehl.", calorificValue: 1480m, protein: 10m);

        var response = await IngredientHandlers.Show(id)(AnEmptyRequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("Mehl", response.Body);
        Assert.Contains("Feines Weizenmehl.", response.Body);
        Assert.Contains("ingredient.section.nutrition", response.Body);
        Assert.Contains("ingredient.field.protein", response.Body);
        Assert.Contains("1480", response.Body);
    }

    [Fact]
    public async Task Show_renders_the_image_when_present()
    {
        var env = BuildEnv();
        var id = await SeedComplete(env, "Mehl", "Feines Weizenmehl.", image: "/images/7");

        var response = await IngredientHandlers.Show(id)(AnEmptyRequest())(env);

        Assert.Contains("src=\"/images/7\"", response.Body);
    }

    [Fact]
    public async Task Show_renders_a_breadcrumb_back_to_the_ingredient_index()
    {
        var env = BuildEnv();
        var id = await SeedComplete(env, "Mehl", "Feines Weizenmehl.");

        var response = await IngredientHandlers.Show(id)(AnEmptyRequest())(env);

        Assert.Contains("class=\"breadcrumb\"", response.Body);
        Assert.Contains("href=\"/ingredients\"", response.Body);
    }

    [Fact]
    public async Task Show_offers_an_edit_link_to_an_editor()
    {
        var env = BuildEnv(Editor());
        var id = await SeedComplete(env, "Mehl", "Feines Weizenmehl.");

        var response = await IngredientHandlers.Show(id)(AnEmptyRequest())(env);

        Assert.Contains($"href=\"/admin/ingredients/{id.Value}/edit\"", response.Body);
    }

    [Fact]
    public async Task Show_hides_the_edit_link_from_a_guest()
    {
        var env = BuildEnv();
        var id = await SeedComplete(env, "Mehl", "Feines Weizenmehl.");

        var response = await IngredientHandlers.Show(id)(AnEmptyRequest())(env);

        Assert.DoesNotContain("/admin/ingredients/", response.Body);
    }

    [Fact]
    public async Task Index_lists_ingredients_linking_to_their_detail_pages()
    {
        var env = BuildEnv();
        var id = await SeedComplete(env, "Mehl", "Feines Weizenmehl.");

        var response = await IngredientHandlers.Index(AnEmptyRequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("Mehl", response.Body);
        Assert.Contains($"href=\"/ingredients/{id.Value}\"", response.Body);
    }

    [Fact]
    public async Task Index_paginates_long_lists()
    {
        var env = BuildEnv();

        // Comfortably more than one page, whatever the configured page size.
        for (var i = 0; i < 61; i++)
        {
            await SeedComplete(env, $"Zutat {i:D2}", "Beschreibung.");
        }

        var response = await IngredientHandlers.Index(AnEmptyRequest())(env);

        Assert.Contains("/ingredients?page=2", response.Body);
    }

    private static AuthenticatedUser EditorUser()
    {
        var user = User.Create(
            new UserId(1),
            new Email("admin@blog.de"),
            new DisplayName("Admin"),
            "hash",
            ["Editor"],
            DateTimeOffset.UtcNow);
        var role = Role.Create(new RoleId(1), "Editor").AddRule(new PermissionRule("Edit", "ingredient"));
        return new AuthenticatedUser(user, [role]);
    }

    private static IPrincipal Editor() => EditorUser();

    private static async Task<IngredientId> SeedComplete(
        Env env,
        string name,
        string description,
        string image = "",
        decimal calorificValue = 0m,
        decimal protein = 0m)
    {
        var id = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            id,
            new IngredientName(name),
            image,
            description,
            density: 1m,
            pieceCount: 0m,
            calorificValue: calorificValue,
            protein: protein,
            fat: 0m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m));
        return id;
    }

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/ingredients", Empty, Empty, Empty, Empty);

    private static Env BuildEnv(IPrincipal? currentUser = null) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: currentUser ?? Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository());

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
