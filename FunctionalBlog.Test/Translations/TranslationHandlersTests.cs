namespace FunctionalBlog.Test.Translations;

public sealed class TranslationHandlersTests
{
    [Fact]
    public async Task SetLanguage_sets_a_secure_language_cookie_and_redirects_to_the_referer()
    {
        var request = new Request(
            HttpMethod.Post,
            "/language",
            new Dictionary<string, string> { ["Referer"] = "/recipes/3" },
            Empty,
            new Dictionary<string, string> { ["lang"] = "en" },
            Empty);

        var response = await TranslationHandlers.SetLanguage(request)(BuildEnv());

        Assert.Equal(303, response.Status);
        Assert.Equal("/recipes/3", response.Headers["Location"]);
        Assert.Contains(response.SetCookies, c => c.StartsWith("lang=en;"));
        Assert.All(response.SetCookies, c => Assert.EndsWith("; Secure", c));
    }

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
