namespace Foodblog.Test.Theme;

public sealed class ThemeMiddlewareTests
{
    [Fact]
    public async Task Sets_theme_from_theme_cookie()
    {
        var captured = string.Empty;
        App inner = _ => env =>
        {
            captured = env.Theme;
            return ValueTask.FromResult(Response.Text("ok"));
        };
        var middleware = ThemeMiddleware.Create()(inner);

        await middleware(RequestWithCookie("theme", "dark"))(BuildEnv());

        Assert.Equal("dark", captured);
    }

    [Fact]
    public async Task Defaults_to_light_when_no_theme_cookie()
    {
        var captured = string.Empty;
        App inner = _ => env =>
        {
            captured = env.Theme;
            return ValueTask.FromResult(Response.Text("ok"));
        };
        var middleware = ThemeMiddleware.Create()(inner);

        await middleware(new Request(HttpMethod.Get, "/", Empty, Empty, Empty, Empty))(BuildEnv());

        Assert.Equal("light", captured);
    }

    [Fact]
    public async Task Ignores_an_unknown_theme_cookie()
    {
        var captured = string.Empty;
        App inner = _ => env =>
        {
            captured = env.Theme;
            return ValueTask.FromResult(Response.Text("ok"));
        };
        var middleware = ThemeMiddleware.Create()(inner);

        await middleware(RequestWithCookie("theme", "neon"))(BuildEnv());

        Assert.Equal("light", captured);
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

    private static Request RequestWithCookie(string name, string value) =>
        new(HttpMethod.Get, "/", Empty, Empty, Empty, new Dictionary<string, string> { [name] = value });

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
