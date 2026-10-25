namespace Foodblog.Test.Roles;

public sealed class AdminHandlerTests
{
    [Fact]
    public async Task UserList_returns_200_for_admin()
    {
        var env = BuildAdminEnv();

        var response = await AdminHandlers.UserList(ARequest())(env);

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task UserList_returns_403_for_regular_user()
    {
        var env = BuildUserEnv();
        var handler = Auth.RequirePermission<Manage>(new UserResource(), AdminHandlers.UserList);

        var response = await handler(ARequest())(env);

        Assert.Equal(403, response.Status);
    }

    [Fact]
    public async Task CreateRole_with_valid_name_saves_role_and_redirects()
    {
        var env = BuildAdminEnv();

        var response = await AdminHandlers.CreateRole(RoleRequest("Autor"))(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/admin/roles", response.Headers["Location"]);
        FunctionalAssert.Some(await env.Roles.FindByName("Autor"));
    }

    [Fact]
    public async Task CreateRole_with_empty_name_returns_400()
    {
        var env = BuildAdminEnv();

        var response = await AdminHandlers.CreateRole(RoleRequest(string.Empty))(env);

        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task AddRule_saves_rule_and_returns_the_new_row_fragment()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor"));

        var response = await AdminHandlers.AddRule(roleId.Value)(RuleRequest("Edit", "article"))(env);

        Assert.Equal(200, response.Status);
        var saved = FunctionalAssert.Some(await env.Roles.FindById(roleId));
        Assert.Contains(new PermissionRule("Edit", "article"), saved.Rules);
        Assert.Contains("rule-row-Edit-article", response.Body);
    }

    [Fact]
    public async Task AddRule_does_not_duplicate_an_existing_rule_and_reports_it()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor").AddRule(new PermissionRule("Edit", "article")));

        var response = await AdminHandlers.AddRule(roleId.Value)(RuleRequest("Edit", "article"))(env);

        Assert.Equal(200, response.Status);
        Assert.Single(FunctionalAssert.Some(await env.Roles.FindById(roleId)).Rules);
        Assert.Contains("admin.roles.rule_exists", response.Body);
        Assert.DoesNotContain("rule-row-Edit-article", response.Body);
    }

    [Fact]
    public async Task DeleteRule_removes_rule_from_role()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        var rule = new PermissionRule("Edit", "article");
        await env.Roles.Save(Role.Create(roleId, "Autor").AddRule(rule));

        var response = await AdminHandlers.DeleteRule(roleId.Value)(RuleRequest("Edit", "article"))(env);

        Assert.Equal(200, response.Status);
        var saved = FunctionalAssert.Some(await env.Roles.FindById(roleId));
        Assert.Empty(saved.Rules);
    }

    [Fact]
    public async Task RoleList_shows_a_new_role_button_and_a_deletable_row_per_role()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor"));

        var response = await AdminHandlers.RoleList(ARequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("href=\"/admin/roles/new\"", response.Body);
        Assert.Contains($"id=\"role-row-{roleId.Value}\"", response.Body);
        Assert.Contains($"/admin/roles/{roleId.Value}/delete", response.Body);
    }

    [Fact]
    public async Task RoleDetail_renders_the_rules_table_with_an_add_rule_button()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor").AddRule(new PermissionRule("Edit", "article")));

        var response = await AdminHandlers.RoleDetail(roleId.Value)(ARequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("rule-row-Edit-article", response.Body);

        // The add affordance is a button that reveals the inline editor — not a persistent input row.
        Assert.Contains($"hx-get=\"/admin/roles/{roleId.Value}/rules/new-row\"", response.Body);
        Assert.DoesNotContain("id=\"rule-add-row\"", response.Body);
    }

    [Fact]
    public async Task NewRuleRow_returns_the_inline_editor_with_selects_and_a_save_post()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor"));

        var response = await AdminHandlers.NewRuleRow(roleId.Value)(ARequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("id=\"rule-add-row\"", response.Body);
        Assert.Contains("<select name=\"action\">", response.Body);
        Assert.Contains("<select name=\"resource\">", response.Body);
        Assert.Contains($"hx-post=\"/admin/roles/{roleId.Value}/rules\"", response.Body);
        Assert.Contains("hx-include=\"closest .rule-add-row\"", response.Body);
    }

    [Fact]
    public async Task NewRuleRow_lists_resources_beyond_the_old_hard_coded_four()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor"));

        var response = await AdminHandlers.NewRuleRow(roleId.Value)(ARequest())(env);

        Assert.Contains("value=\"recipe\"", response.Body);
        Assert.Contains("value=\"ingredient\"", response.Body);
        Assert.Contains("value=\"settings\"", response.Body);
    }

    [Fact]
    public async Task RoleDetail_orders_rules_by_resource()
    {
        var env = BuildAdminEnv();
        var roleId = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(roleId, "Autor")
            .AddRule(new PermissionRule("Edit", "user"))
            .AddRule(new PermissionRule("Edit", "article")));

        var response = await AdminHandlers.RoleDetail(roleId.Value)(ARequest())(env);

        var articleAt = response.Body.IndexOf("rule-row-Edit-article", StringComparison.Ordinal);
        var userAt = response.Body.IndexOf("rule-row-Edit-user", StringComparison.Ordinal);
        Assert.True(articleAt < userAt, "article rule should render before user rule");
    }

    [Fact]
    public async Task UpdateUserRoles_assigns_roles_to_user()
    {
        var env = BuildAdminEnv();
        var userId = await env.Users.NextId();
        await env.Users.Save(User.Create(userId, new Email("user@blog.de"), new DisplayName("Testbenutzer"), "hash", [], DateTimeOffset.UtcNow));

        var response = await AdminHandlers.UpdateUserRoles(userId.Value)(
            AssignRolesRequest("Admin"))(env);

        Assert.Equal(303, response.Status);
        var saved = FunctionalAssert.Some(await env.Users.FindById(userId));
        Assert.Contains("Admin", saved.RoleNames);
    }

    private static Env BuildAdminEnv()
    {
        var adminRule = new PermissionRule("Manage", "user");
        var roleRule = new PermissionRule("Manage", "role");
        var ruleRule = new PermissionRule("Manage", "rule");
        var role = new Role(new RoleId(1), "Admin", [adminRule, roleRule, ruleRule]);
        var user = User.Create(new UserId(1), new Email("admin@blog.de"), new DisplayName("Admin"), "hash", ["Admin"], DateTimeOffset.UtcNow);
        var principal = new AuthenticatedUser(user, [role]);
        return BuildEnv(principal);
    }

    private static Env BuildUserEnv()
    {
        var user = User.Create(new UserId(2), new Email("user@blog.de"), new DisplayName("Testbenutzer"), "hash", [], DateTimeOffset.UtcNow);
        var principal = new AuthenticatedUser(user, []);
        return BuildEnv(principal);
    }

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

    private static Request ARequest() =>
        new(HttpMethod.Get, "/admin/users", Empty, Empty, Empty, Empty);

    private static Request RoleRequest(string name) =>
        new(HttpMethod.Post, "/admin/roles", Empty, Empty, new Dictionary<string, string> { ["name"] = name }, Empty);

    private static Request RuleRequest(string action, string resource) =>
        new(
            HttpMethod.Post,
            "/admin/roles/1/rules",
            Empty,
            Empty,
            new Dictionary<string, string> { ["action"] = action, ["resource"] = resource },
            Empty);

    private static Request AssignRolesRequest(string roleName) =>
        new(
            HttpMethod.Post,
            "/admin/users/1/roles",
            Empty,
            Empty,
            new Dictionary<string, string> { ["role"] = roleName },
            Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
