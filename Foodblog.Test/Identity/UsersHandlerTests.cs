namespace Foodblog.Test.Identity;

public sealed class UsersHandlerTests
{
    [Fact]
    public async Task Index_lists_every_user_with_their_join_year()
    {
        var env = BuildEnv();
        await SeedUser(env, 1, "Thomas", 2016);
        await SeedUser(env, 2, "Sabrina", 2018);

        var response = await UsersHandlers.Index(AnEmptyRequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("Thomas", response.Body);
        Assert.Contains("Sabrina", response.Body);
        Assert.Contains("2016", response.Body);
        Assert.Contains("user-card", response.Body);
    }

    [Fact]
    public async Task Index_flags_users_with_recipes_as_authors()
    {
        var env = BuildEnv();
        var author = await SeedUser(env, 1, "Thomas", 2016);
        await SeedUser(env, 2, "Sabrina", 2018);
        await SeedRecipe(env, author.Id);

        var response = await UsersHandlers.Index(AnEmptyRequest())(env);

        // Exactly one author badge — only Thomas has a recipe.
        Assert.Contains("users.author_badge", response.Body);
        Assert.Equal(1, Occurrences(response.Body, "users.author_badge"));
    }

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static async Task<User> SeedUser(Env env, int id, string name, int joinYear)
    {
        var user = User.Create(
            new UserId(id),
            new Email($"user{id}@blog.de"),
            new DisplayName(name),
            "hash",
            [],
            new DateTimeOffset(joinYear, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await env.Users.Save(user);
        return user;
    }

    private static async Task SeedRecipe(Env env, UserId authorId)
    {
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein klassischer Rührkuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            authorId,
            Difficulty.Easy,
            [],
            4,
            [],
            [],
            []));
    }

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/users", Empty, Empty, Empty, Empty);

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
