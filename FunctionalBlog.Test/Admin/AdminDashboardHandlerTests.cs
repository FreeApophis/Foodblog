namespace FunctionalBlog.Test.Admin;

public sealed class AdminDashboardHandlerTests
{
    [Fact]
    public async Task Dashboard_returns_200_for_an_admin()
    {
        var env = BuildEnv(FullAdmin());

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task Dashboard_shows_a_card_for_every_section_the_admin_can_reach()
    {
        var env = BuildEnv(FullAdmin());

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.Contains("href=\"/admin/users\"", response.Body);
        Assert.Contains("href=\"/admin/units\"", response.Body);
        Assert.Contains("href=\"/admin/roles\"", response.Body);
        Assert.Contains("href=\"/admin/ingredients\"", response.Body);
        Assert.Contains("href=\"/admin/translations\"", response.Body);

        // Images, Pages and BlogEntries point at routes that also appear in the nav, so
        // assert on their (nav-absent) card title keys instead.
        Assert.Contains("admin.dashboard.card.images", response.Body);
        Assert.Contains("admin.dashboard.card.pages", response.Body);
        Assert.Contains("admin.dashboard.card.blog", response.Body);
    }

    [Fact]
    public async Task Dashboard_hides_cards_the_user_has_no_permission_for()
    {
        var env = BuildEnv(UnitOnlyAdmin());

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.Contains("href=\"/admin/units\"", response.Body);
        Assert.DoesNotContain("href=\"/admin/users\"", response.Body);
        Assert.DoesNotContain("href=\"/admin/roles\"", response.Body);
        Assert.DoesNotContain("admin.dashboard.card.blog", response.Body);
    }

    [Fact]
    public async Task Dashboard_shows_content_counts_for_accessible_sections()
    {
        var env = BuildEnv(FullAdmin());

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.Contains("admin.dashboard.stat.articles", response.Body);
        Assert.Contains("admin.dashboard.stat.ingredients", response.Body);
        Assert.Contains("admin.dashboard.stat.users", response.Body);
    }

    [Fact]
    public async Task Dashboard_flags_incomplete_ingredients_as_needing_attention()
    {
        var ingredients = new InMemoryIngredientRepository();
        var id = await ingredients.NextId();
        await ingredients.Save(Ingredient.Create(
            id,
            new IngredientName("Stub"),
            image: string.Empty,
            description: string.Empty,
            density: 1m,
            pieceCount: 0m,
            calorificValue: 0m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m));

        var env = BuildEnv(FullAdmin(), ingredients);

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.Contains("admin.dashboard.attention.incomplete_ingredients", response.Body);
    }

    [Fact]
    public async Task Dashboard_warns_when_the_site_url_is_not_set()
    {
        var env = BuildEnv(FullAdmin(), siteUrl: string.Empty);

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.Contains("admin.dashboard.attention.site_url_missing", response.Body);
        Assert.Contains("href=\"/admin/settings\"", response.Body);
    }

    [Fact]
    public async Task Dashboard_does_not_warn_when_the_site_url_is_set()
    {
        var env = BuildEnv(FullAdmin(), siteUrl: "https://foodblog.ch");

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.DoesNotContain("admin.dashboard.attention.site_url_missing", response.Body);
    }

    [Fact]
    public async Task Dashboard_hides_the_site_url_warning_from_users_who_cannot_manage_settings()
    {
        var env = BuildEnv(UnitOnlyAdmin(), siteUrl: string.Empty);

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.DoesNotContain("admin.dashboard.attention.site_url_missing", response.Body);
    }

    [Fact]
    public async Task Dashboard_has_no_attention_notice_when_no_ingredients_are_incomplete()
    {
        var env = BuildEnv(FullAdmin());

        var response = await AdminDashboardHandlers.Dashboard(ARequest())(env);

        Assert.DoesNotContain("admin.dashboard.attention.incomplete_ingredients", response.Body);
    }

    private static AuthenticatedUser FullAdmin()
    {
        var rules = new[]
        {
            new PermissionRule("Manage", "user"),
            new PermissionRule("Manage", "role"),
            new PermissionRule("Manage", "unit"),
            new PermissionRule("Manage", "ingredient"),
            new PermissionRule("Manage", "image"),
            new PermissionRule("Create", "page"),
            new PermissionRule("Create", "article"),
            new PermissionRule("Manage", "settings"),
        };
        var role = new Role(new RoleId(1), "Admin", rules);
        var user = User.Create(new UserId(1), new Email("admin@blog.de"), new DisplayName("Admin"), "hash", ["Admin"], DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, [role]);
    }

    private static AuthenticatedUser UnitOnlyAdmin()
    {
        var role = new Role(new RoleId(2), "UnitAdmin", [new PermissionRule("Manage", "unit")]);
        var user = User.Create(new UserId(2), new Email("units@blog.de"), new DisplayName("Units"), "hash", ["UnitAdmin"], DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, [role]);
    }

    private static Env BuildEnv(IPrincipal principal, IIngredientRepository? ingredients = null, string siteUrl = "https://foodblog.ch") => new(
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
        Ingredients: ingredients ?? new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Config: TestConfiguration.With((ConfigurationKeys.SiteUrl, siteUrl)));

    private static Request ARequest() =>
        new(HttpMethod.Get, "/admin", Empty, Empty, Empty, Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
