namespace Foodblog.Test.Identity;

public sealed class AuthHelperTests
{
    [Fact]
    public async Task RequireAuth_redirects_guest_to_login()
    {
        var env = BuildEnv(Guest.Instance);
        var handler = Auth.RequireAuth(OkHandler);

        var response = await handler(ARequest())(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task RequireAuth_calls_inner_handler_for_authenticated_user()
    {
        var env = BuildEnv(AuthUser());
        var handler = Auth.RequireAuth(OkHandler);

        var response = await handler(ARequest())(env);

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task RequirePermission_redirects_guest_to_login()
    {
        var env = BuildEnv(Guest.Instance);
        var handler = Auth.RequirePermission<Edit>(new ArticleResource(), OkHandler);

        var response = await handler(ARequest())(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task RequirePermission_returns_403_for_authenticated_user_without_permission()
    {
        var env = BuildEnv(AuthUser());
        var handler = Auth.RequirePermission<Edit>(new ArticleResource(), OkHandler);

        var response = await handler(ARequest())(env);

        Assert.Equal(403, response.Status);
    }

    [Fact]
    public async Task RequirePermission_calls_inner_handler_when_permission_is_granted()
    {
        var rule = new PermissionRule("Edit", "article");
        var role = new Role(new RoleId(1), "Autor", [rule]);
        var env = BuildEnv(AuthUser([role]));
        var handler = Auth.RequirePermission<Edit>(new ArticleResource(), OkHandler);

        var response = await handler(ARequest())(env);

        Assert.Equal(200, response.Status);
    }

    private static App OkHandler => _ => _ => ValueTask.FromResult(Response.Text("ok"));

    private static Request ARequest() =>
        new(HttpMethod.Get, "/", new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>());

    private static Env BuildEnv(IPrincipal principal) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: principal,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository());

    private static AuthenticatedUser AuthUser(IReadOnlyList<Role>? roles = null)
    {
        var user = User.Create(
            new UserId(1),
            new Email("test@blog.de"),
            new DisplayName("Testbenutzer"),
            "hash",
            [],
            DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, roles ?? []);
    }
}
