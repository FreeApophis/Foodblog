namespace Foodblog.Test.Tags;

public sealed class TagHandlerTests
{
    [Fact]
    public async Task Show_lists_recipes_carrying_the_tag()
    {
        var env = BuildEnv();
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Schoko Kuchen"),
            new RecipeDescription("Beschreibung"),
            [],
            new UserId(1),
            Difficulty.Easy,
            [new RecipeTag("Süss")],
            2,
            [],
            [],
            []));

        var response = await TagHandlers.Show("suess")(AGetRequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("Schoko Kuchen", response.Body);
        Assert.Contains("recipe-card", response.Body);
    }

    [Fact]
    public async Task Show_normalizes_the_path_segment_to_a_slug()
    {
        var env = BuildEnv();

        // "Süss" must resolve to the "suess" tag just like the canonical slug does.
        var response = await TagHandlers.Show("Süss")(AGetRequest())(env);

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task Show_returns_404_for_an_unknown_tag()
    {
        var env = BuildEnv();

        var response = await TagHandlers.Show("gibtsnicht")(AGetRequest())(env);

        Assert.Equal(404, response.Status);
    }

    private static Request AGetRequest() =>
        new(HttpMethod.Get, "/tag/suess", Empty, Empty, Empty, Empty);

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
        Pages: new InMemoryPageRepository(),
        Tags: new InMemoryTagRepository(new Tag("suess", "süss")));

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
