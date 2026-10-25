namespace Foodblog.Test.Identity;

public sealed class AuthMiddlewareTests
{
    [Fact]
    public async Task No_session_cookie_results_in_Guest_principal()
    {
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/", EmptyDict, EmptyDict, EmptyDict, EmptyDict);

        var resolved = await AuthMiddleware.ResolvePrincipal(request, env);

        Assert.IsType<Guest>(resolved);
    }

    [Fact]
    public async Task Unknown_session_token_results_in_Guest_principal()
    {
        var env = BuildEnv();
        var request = RequestWithCookie("session", "unknown-token");

        var resolved = await AuthMiddleware.ResolvePrincipal(request, env);

        Assert.IsType<Guest>(resolved);
    }

    [Fact]
    public async Task Expired_session_results_in_Guest_principal()
    {
        var env = BuildEnv();
        var userId = await env.Users.NextId();
        var user = User.Create(userId, new Email("test@blog.de"), new DisplayName("Testbenutzer"), "hash", [], DateTimeOffset.UtcNow);
        await env.Users.Save(user);

        var expired = new Session("tok", userId, DateTimeOffset.UtcNow.AddMinutes(-1));
        await env.Sessions.Save(expired);

        var request = RequestWithCookie("session", "tok");

        var resolved = await AuthMiddleware.ResolvePrincipal(request, env);

        Assert.IsType<Guest>(resolved);
    }

    [Fact]
    public async Task Valid_session_results_in_AuthenticatedUser_with_correct_email()
    {
        var env = BuildEnv();
        var userId = await env.Users.NextId();
        var user = User.Create(userId, new Email("test@blog.de"), new DisplayName("Testbenutzer"), "hash", [], DateTimeOffset.UtcNow);
        await env.Users.Save(user);

        var session = new Session("tok", userId, DateTimeOffset.UtcNow.AddDays(30));
        await env.Sessions.Save(session);

        var request = RequestWithCookie("session", "tok");

        var resolved = await AuthMiddleware.ResolvePrincipal(request, env);

        var authenticated = Assert.IsType<AuthenticatedUser>(resolved);
        Assert.Equal("test@blog.de", authenticated.Email.Value);
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
        new(HttpMethod.Get, "/", EmptyDict, EmptyDict, EmptyDict, new Dictionary<string, string> { [name] = value });

    private static readonly IReadOnlyDictionary<string, string> EmptyDict =
        new Dictionary<string, string>();
}
