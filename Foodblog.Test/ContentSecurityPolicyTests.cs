namespace Foodblog.Test;

public sealed class ContentSecurityPolicyTests
{
    private const string ExpectedPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; " +
        "font-src 'self'; connect-src 'self'; frame-src https://www.instagram.com; object-src 'none'; base-uri 'self'; " +
        "form-action 'self'; frame-ancestors 'self'";

    [Fact]
    public async Task Html_responses_carry_the_policy()
    {
        var app = ContentSecurityPolicy.Create()(Respond(Response.Html("<p>Hallo</p>")));

        var response = await app(Get("/"))(Env());

        Assert.Equal(ExpectedPolicy, response.Headers["Content-Security-Policy"]);
    }

    [Fact]
    public async Task Html_error_pages_carry_the_policy()
    {
        var app = ContentSecurityPolicy.Create()(Respond(Response.Html("<p>Fehler</p>", 500)));

        var response = await app(Get("/"))(Env());

        Assert.True(response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Non_html_responses_are_left_alone()
    {
        var app = ContentSecurityPolicy.Create()(Respond(Response.Css("body {}")));

        var response = await app(Get("/styles.css"))(Env());

        Assert.False(response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Existing_headers_are_kept()
    {
        var app = ContentSecurityPolicy.Create()(Respond(Response.Html("<p>Hallo</p>").WithHeader("X-Test", "1")));

        var response = await app(Get("/"))(Env());

        Assert.Equal("1", response.Headers["X-Test"]);
    }

    private static App Respond(Response response) => _ => _ => ValueTask.FromResult(response);

    private static Request Get(string path) => new(HttpMethod.Get, path, Empty, Empty, Empty, Empty);

    private static Env Env() => new(
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
