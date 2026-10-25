namespace FunctionalBlog.Test;

public class CsrfMiddlewareTests
{
    [Fact]
    public async Task Get_passes_through_without_validation()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Get("/");

        var response = await app(request)(BuildEnv());

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task Post_with_matching_token_passes_through()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Post("/", form: [("_csrf", "abc123")], cookies: [("_csrf", "abc123")]);

        var response = await app(request)(BuildEnv());

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task Post_with_wrong_form_token_returns_403()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Post("/", form: [("_csrf", "wrong")], cookies: [("_csrf", "correct")]);

        var response = await app(request)(BuildEnv());

        Assert.Equal(403, response.Status);
    }

    [Fact]
    public async Task Post_with_missing_form_token_returns_403()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Post("/", form: [], cookies: [("_csrf", "some-token")]);

        var response = await app(request)(BuildEnv());

        Assert.Equal(403, response.Status);
    }

    [Fact]
    public async Task Post_with_missing_cookie_returns_403()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Post("/", form: [("_csrf", "some-token")], cookies: []);

        var response = await app(request)(BuildEnv());

        Assert.Equal(403, response.Status);
    }

    [Fact]
    public async Task Sets_csrf_cookie_when_absent()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Get("/");

        var response = await app(request)(BuildEnv());

        Assert.Contains(response.SetCookies, c => c.StartsWith("_csrf="));
        Assert.All(response.SetCookies, c => Assert.EndsWith("; Secure", c));
    }

    [Fact]
    public async Task Does_not_reset_existing_csrf_cookie()
    {
        var app = CsrfMiddleware.Create()(PassThrough200);
        var request = Get("/", cookies: [("_csrf", "existing-token")]);

        var response = await app(request)(BuildEnv());

        Assert.Empty(response.SetCookies);
    }

    [Fact]
    public async Task Csrf_token_is_placed_on_env()
    {
        string? captured = null;
        App capture = _ => env =>
        {
            captured = env.CsrfToken;
            return ValueTask.FromResult(Response.Text("ok"));
        };
        var app = CsrfMiddleware.Create()(capture);
        var request = Get("/", cookies: [("_csrf", "my-token")]);

        await app(request)(BuildEnv());

        Assert.Equal("my-token", captured);
    }

    private static Request Get(string path, (string, string)[]? cookies = null) =>
        new(HttpMethod.Get, path, Empty, Empty, Empty, ToDictionary(cookies ?? []));

    private static Request Post(string path, (string, string)[] form, (string, string)[] cookies) =>
        new(HttpMethod.Post, path, Empty, Empty, ToDictionary(form), ToDictionary(cookies));

    private static IReadOnlyDictionary<string, string> ToDictionary((string, string)[] pairs) =>
        pairs.ToDictionary(p => p.Item1, p => p.Item2);

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

    private static readonly App PassThrough200 = _ => _ => ValueTask.FromResult(Response.Text("ok"));
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
