namespace Foodblog.Test;

public sealed class SeederTests
{
    private const string LeakedDefaultPassword = "Admin1234";
    private const string BootstrapPasswordMarker = "bootstrap-admin-password:";

    [Fact]
    public async Task SeedAsync_bootstraps_exactly_one_admin_on_an_empty_database()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var admin = Assert.Single(await env.Users.All());
        Assert.Equal("admin@blog.de", admin.Email.Value);
        Assert.Contains("Admin", admin.RoleNames);
        Assert.False(env.PasswordHasher.Verify(LeakedDefaultPassword, admin.PasswordHash));
    }

    [Fact]
    public async Task SeedAsync_logs_a_random_bootstrap_password_that_verifies_against_the_stored_hash()
    {
        var log = new TestLog();
        var env = BuildEnv(log);

        await Seeder.SeedAsync(env);

        var admin = Assert.Single(await env.Users.All());
        var password = ExtractBootstrapPassword(log);
        Assert.True(password.Length >= 20);
        Assert.NotEqual(LeakedDefaultPassword, password);
        Assert.True(env.PasswordHasher.Verify(password, admin.PasswordHash));
    }

    [Fact]
    public async Task SeedAsync_generates_a_different_bootstrap_password_per_database()
    {
        var log1 = new TestLog();
        var log2 = new TestLog();

        await Seeder.SeedAsync(BuildEnv(log1));
        await Seeder.SeedAsync(BuildEnv(log2));

        Assert.NotEqual(ExtractBootstrapPassword(log1), ExtractBootstrapPassword(log2));
    }

    [Fact]
    public async Task SeedAsync_honours_the_bootstrap_admin_email()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env, Option.Some("owner@example.org"));

        var admin = Assert.Single(await env.Users.All());
        Assert.Equal("owner@example.org", admin.Email.Value);
    }

    [Fact]
    public async Task SeedAsync_falls_back_to_the_default_email_when_the_bootstrap_email_is_invalid()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env, Option.Some("not-an-email"));

        var admin = Assert.Single(await env.Users.All());
        Assert.Equal("admin@blog.de", admin.Email.Value);
    }

    [Fact]
    public async Task SeedAsync_creates_no_user_when_any_user_exists()
    {
        var log = new TestLog();
        var env = BuildEnv(log);
        var existing = await SaveUser(env, "jemand@example.org", "geheimes-passwort", ["Benutzer"]);

        await Seeder.SeedAsync(env);

        Assert.Equal([existing], await env.Users.All());
        Assert.DoesNotContain(log.Messages, m => m.Contains(BootstrapPasswordMarker));
    }

    [Fact]
    public async Task SeedAsync_adds_no_sample_content_when_any_user_exists()
    {
        var env = BuildEnv();
        await SaveUser(env, "jemand@example.org", "geheimes-passwort", ["Benutzer"]);

        await Seeder.SeedAsync(env);

        Assert.Empty(await env.Articles.All());
        Assert.Empty(await env.Recipes.All());
    }

    [Fact]
    public async Task SeedAsync_locks_the_known_default_admin_account()
    {
        var log = new TestLog();
        var env = BuildEnv(log);
        var admin = await SaveUser(env, "admin@blog.de", LeakedDefaultPassword, ["Admin"]);
        var recipe = await SaveRecipe(env, admin.Id);
        await env.Sessions.Save(new Session("admin-session", admin.Id, DateTimeOffset.UtcNow.AddDays(30)));

        await Seeder.SeedAsync(env);

        var locked = FunctionalAssert.Some(await env.Users.FindById(admin.Id));
        Assert.False(env.PasswordHasher.Verify(LeakedDefaultPassword, locked!.PasswordHash));
        Assert.Equal(admin.Email, locked.Email);
        Assert.Equal(admin.RoleNames, locked.RoleNames);
        FunctionalAssert.None(await env.Sessions.Find("admin-session"));
        Assert.Equal(Option.Some(recipe), await env.Recipes.Find(recipe.Id));
        Assert.Contains(log.Messages, m => m.Contains("WARNING") && m.Contains("admin@blog.de"));
    }

    [Fact]
    public async Task SeedAsync_leaves_the_default_admin_email_alone_when_its_password_was_changed()
    {
        var log = new TestLog();
        var env = BuildEnv(log);
        var admin = await SaveUser(env, "admin@blog.de", "ein-eigenes-passwort", ["Admin"]);
        await env.Sessions.Save(new Session("admin-session", admin.Id, DateTimeOffset.UtcNow.AddDays(30)));

        await Seeder.SeedAsync(env);

        var unchanged = FunctionalAssert.Some(await env.Users.FindById(admin.Id));
        Assert.Equal(admin.PasswordHash, unchanged!.PasswordHash);
        FunctionalAssert.Some(await env.Sessions.Find("admin-session"));
        Assert.DoesNotContain(log.Messages, m => m.Contains("WARNING"));
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_for_users_when_called_twice()
    {
        var log = new TestLog();
        var env = BuildEnv(log);

        await Seeder.SeedAsync(env);
        var afterFirst = await env.Users.All();
        await Seeder.SeedAsync(env);

        Assert.Equal(afterFirst, await env.Users.All());
        Assert.Single(log.Messages, m => m.Contains(BootstrapPasswordMarker));
    }

    [Fact]
    public async Task SeedAsync_gives_the_seeded_admin_without_a_password_a_one_time_password()
    {
        var log = new TestLog();
        var env = BuildEnv(log);
        var seeded = await SavePasswordlessAdmin(env);

        await Seeder.SeedAsync(env);

        var admin = Assert.Single(await env.Users.All());
        Assert.Equal(seeded.Id, admin.Id);
        Assert.Equal(seeded.Email, admin.Email);
        Assert.True(env.PasswordHasher.Verify(ExtractBootstrapPassword(log), admin.PasswordHash));
    }

    [Fact]
    public async Task SeedAsync_moves_the_seeded_admin_to_the_bootstrap_admin_email()
    {
        var env = BuildEnv();
        var seeded = await SavePasswordlessAdmin(env);

        await Seeder.SeedAsync(env, Option.Some("owner@example.org"));

        var admin = Assert.Single(await env.Users.All());
        Assert.Equal(seeded.Id, admin.Id);
        Assert.Equal("owner@example.org", admin.Email.Value);
    }

    [Fact]
    public async Task SeedAsync_sets_the_seeded_admin_password_only_once()
    {
        var log = new TestLog();
        var env = BuildEnv(log);
        await SavePasswordlessAdmin(env);

        await Seeder.SeedAsync(env);
        var afterFirst = await env.Users.All();
        await Seeder.SeedAsync(env);

        Assert.Equal(afterFirst, await env.Users.All());
        Assert.Single(log.Messages, m => m.Contains(BootstrapPasswordMarker));
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_when_locking_the_default_admin()
    {
        var env = BuildEnv();
        var admin = await SaveUser(env, "admin@blog.de", LeakedDefaultPassword, ["Admin"]);

        await Seeder.SeedAsync(env);
        var afterFirst = await env.Users.All();
        await Seeder.SeedAsync(env);

        Assert.Equal(afterFirst, await env.Users.All());
        Assert.Equal([admin.Id], afterFirst.Select(u => u.Id));
    }

    [Fact]
    public async Task SeedAsync_creates_admin_role_with_manage_permissions()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var role = FunctionalAssert.Some(await env.Roles.FindByName("Admin"));

        Assert.Contains(new PermissionRule("Manage", "user"), role!.Rules);
        Assert.Contains(new PermissionRule("Manage", "role"), role.Rules);
        Assert.Contains(new PermissionRule("Manage", "rule"), role.Rules);
    }

    [Fact]
    public async Task SeedAsync_grants_admin_image_permissions()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var role = FunctionalAssert.Some(await env.Roles.FindByName("Admin"));

        Assert.Contains(new PermissionRule("Create", "image"), role!.Rules);
        Assert.Contains(new PermissionRule("Delete", "image"), role.Rules);
        Assert.Contains(new PermissionRule("Manage", "image"), role.Rules);
    }

    [Fact]
    public async Task SeedAsync_grants_admin_page_permissions()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var role = FunctionalAssert.Some(await env.Roles.FindByName("Admin"));

        Assert.Contains(new PermissionRule("Create", "page"), role!.Rules);
        Assert.Contains(new PermissionRule("Edit", "page"), role.Rules);
        Assert.Contains(new PermissionRule("Delete", "page"), role.Rules);
    }

    [Fact]
    public async Task SeedAsync_adds_missing_image_permissions_to_a_preexisting_admin_role()
    {
        var env = BuildEnv();
        var id = await env.Roles.NextId();
        await env.Roles.Save(Role.Create(id, "Admin").AddRule(new PermissionRule("Manage", "user")));

        await Seeder.SeedAsync(env);

        var role = FunctionalAssert.Some(await env.Roles.FindByName("Admin"));
        Assert.Contains(new PermissionRule("Manage", "image"), role!.Rules);
        Assert.Contains(new PermissionRule("Create", "image"), role.Rules);
    }

    [Fact]
    public async Task SeedAsync_does_not_duplicate_rules_when_called_twice()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);
        await Seeder.SeedAsync(env);

        var role = FunctionalAssert.Some(await env.Roles.FindByName("Admin"));
        Assert.Equal(role!.Rules.Distinct().Count(), role.Rules.Count);
    }

    [Fact]
    public async Task SeedAsync_creates_default_benutzer_role()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        FunctionalAssert.Some(await env.Roles.FindByName("Benutzer"));
    }

    [Fact]
    public async Task SeedAsync_creates_sample_ingredients()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var ingredients = await env.Ingredients.All();
        Assert.Contains(ingredients, i => i.Name.Value == "Mehl");
        Assert.Contains(ingredients, i => i.Name.Value == "Zucker");
        Assert.Contains(ingredients, i => i.Name.Value == "Butter");
        Assert.Contains(ingredients, i => i.Name.Value == "Eier");
        Assert.Contains(ingredients, i => i.Name.Value == "Milch");
    }

    [Fact]
    public async Task SeedAsync_creates_sample_recipes()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var recipes = await env.Recipes.All();
        Assert.Contains(recipes, r => r.Name.Value == "Einfacher Rührkuchen");
        Assert.Contains(recipes, r => r.Name.Value == "Pfannkuchen");
    }

    [Fact]
    public async Task SeedAsync_creates_kartoffelgulasch_recipe()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var recipes = await env.Recipes.All();
        Assert.Contains(recipes, r => r.Name.Value == "Kartoffelgulasch");
    }

    [Fact]
    public async Task SeedAsync_creates_aelpler_one_pot_recipe()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        var recipes = await env.Recipes.All();
        Assert.Contains(recipes, r => r.Name.Value == "Älpler One-Pot");
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_for_recipes_when_called_twice()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);
        await Seeder.SeedAsync(env);

        var recipes = await env.Recipes.All();
        Assert.Single(recipes, r => r.Name.Value == "Pfannkuchen");
    }

    [Fact]
    public async Task SeedAsync_seeds_default_configuration_keys()
    {
        var env = BuildEnv();

        await Seeder.SeedAsync(env);

        Assert.Equal(Option.Some("foodblog.ch"), await env.Configuration!.Get(ConfigurationKeys.SiteName));
        Assert.Equal(Option.Some("587"), await env.Configuration!.Get(ConfigurationKeys.SmtpPort));
        Assert.Equal(Option.Some(string.Empty), await env.Configuration!.Get(ConfigurationKeys.SmtpHost));
    }

    [Fact]
    public async Task SeedAsync_does_not_overwrite_existing_configuration()
    {
        var env = BuildEnv();
        await env.Configuration!.Set(ConfigurationKeys.SiteName, "Mein eigener Blog");

        await Seeder.SeedAsync(env);

        Assert.Equal(Option.Some("Mein eigener Blog"), await env.Configuration!.Get(ConfigurationKeys.SiteName));
    }

    private static string ExtractBootstrapPassword(TestLog log)
    {
        var line = Assert.Single(log.Messages, m => m.Contains(BootstrapPasswordMarker));
        var start = line.IndexOf(BootstrapPasswordMarker, StringComparison.Ordinal) + BootstrapPasswordMarker.Length;
        return line[start..].Trim().Split(' ')[0];
    }

    private static async Task<User> SaveUser(Env env, string email, string password, IReadOnlyList<string> roleNames)
    {
        var user = User.Create(
            await env.Users.NextId(),
            new Email(email),
            new DisplayName("Admin"),
            env.PasswordHasher.Hash(password),
            roleNames,
            DateTimeOffset.UtcNow);
        await env.Users.Save(user);
        return user;
    }

    // Mirrors the admin the 0002_seed migration ships: it owns the seeded content but has no password yet.
    private static async Task<User> SavePasswordlessAdmin(Env env)
    {
        var user = User.Create(
            await env.Users.NextId(),
            new Email("admin@blog.de"),
            new DisplayName("Admin"),
            string.Empty,
            ["Admin"],
            DateTimeOffset.UtcNow);
        await env.Users.Save(user);
        return user;
    }

    private static async Task<Recipe> SaveRecipe(Env env, UserId authorId)
    {
        var recipe = Recipe.Create(
            await env.Recipes.NextId(),
            new RecipeName("Kuchen"),
            new RecipeDescription("Beschreibung"),
            [new PreparationStep(1, "Backen.")],
            authorId,
            Difficulty.Easy,
            [],
            4,
            [],
            [],
            []);
        await env.Recipes.Save(recipe);
        return recipe;
    }

    private static Env BuildEnv(ILog? log = null) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: log ?? new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Configuration: new InMemoryConfigurationRepository());
}
