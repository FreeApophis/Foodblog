namespace Foodblog.Test.Translations;

public sealed class LanguageMiddlewareTests
{
    [Fact]
    public async Task Sets_language_from_lang_cookie()
    {
        var captured = string.Empty;
        App inner = _ => env =>
        {
            captured = env.Language;
            return ValueTask.FromResult(Response.Text("ok"));
        };
        var middleware = LanguageMiddleware.Create()(inner);

        await middleware(RequestWithCookie("lang", "en"))(BuildEnv());

        Assert.Equal("en", captured);
    }

    [Fact]
    public async Task Defaults_to_de_when_no_lang_cookie()
    {
        var captured = string.Empty;
        App inner = _ => env =>
        {
            captured = env.Language;
            return ValueTask.FromResult(Response.Text("ok"));
        };
        var middleware = LanguageMiddleware.Create()(inner);

        await middleware(new Request(HttpMethod.Get, "/", Empty, Empty, Empty, Empty))(BuildEnv());

        Assert.Equal("de", captured);
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
