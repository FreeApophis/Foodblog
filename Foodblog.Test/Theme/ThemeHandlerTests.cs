namespace Foodblog.Test.Theme;

public sealed class ThemeHandlerTests
{
    [Fact]
    public async Task SetTheme_sets_the_theme_cookie_and_redirects_to_the_referer()
    {
        var request = new Request(
            HttpMethod.Post,
            "/theme",
            new Dictionary<string, string> { ["Referer"] = "/recipes/3" },
            Empty,
            new Dictionary<string, string> { ["theme"] = "dark" },
            Empty);

        var response = await ThemeHandlers.SetTheme(request)(BuildEnv());

        Assert.Equal(303, response.Status);
        Assert.Equal("/recipes/3", response.Headers["Location"]);
        Assert.Contains(response.SetCookies, c => c.StartsWith("theme=dark;"));
        Assert.All(response.SetCookies, c => Assert.EndsWith("; Secure", c));
    }

    [Fact]
    public async Task SetTheme_falls_back_to_light_for_an_unknown_value()
    {
        var request = new Request(
            HttpMethod.Post,
            "/theme",
            Empty,
            Empty,
            new Dictionary<string, string> { ["theme"] = "neon" },
            Empty);

        var response = await ThemeHandlers.SetTheme(request)(BuildEnv());

        Assert.Equal(303, response.Status);
        Assert.Equal("/", response.Headers["Location"]);
        Assert.Contains(response.SetCookies, c => c.StartsWith("theme=light;"));
    }

    [Fact]
    public void Layout_renders_the_active_theme_on_the_html_element()
    {
        var ctx = new ViewContext(Guest.Instance, key => key, string.Empty, "dark");

        var html = Layout.Page("Titel", Html.Raw("<p>x</p>"), ctx);

        Assert.Contains("data-theme=\"dark\"", html);
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
