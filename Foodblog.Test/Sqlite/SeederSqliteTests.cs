namespace Foodblog.Test.Sqlite;

// The 0002_seed migration ships the admin@blog.de account with the publicly known default password,
// so every freshly-migrated (or long-running) database has it. The seeder must lock it on startup.
public sealed class SeederSqliteTests : IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SeedAsync_locks_the_migration_seeded_default_admin()
    {
        var env = BuildEnv();
        var before = FunctionalAssert.Some(await env.Users.FindByEmail(new Email("admin@blog.de")));
        Assert.True(env.PasswordHasher.Verify("Admin1234", before!.PasswordHash));

        await Seeder.SeedAsync(env);

        var after = FunctionalAssert.Some(await env.Users.FindByEmail(new Email("admin@blog.de")));
        Assert.False(env.PasswordHasher.Verify("Admin1234", after!.PasswordHash));
        Assert.Equal(before.Id, after.Id);
    }

    [Fact]
    public async Task SeedAsync_creates_no_user_in_a_migrated_database()
    {
        var env = BuildEnv();
        var before = await env.Users.All();

        await Seeder.SeedAsync(env);

        Assert.Equal(before.Select(u => u.Id), (await env.Users.All()).Select(u => u.Id));
    }

    private Env BuildEnv() => new(
        Articles: new SqliteArticleRepository(_db.Connections),
        Users: new SqliteUserRepository(_db.Connections),
        Roles: new SqliteRoleRepository(_db.Connections),
        Sessions: new SqliteSessionStore(_db.Connections),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new TestLog(),
        CurrentUser: Guest.Instance,
        Recipes: new SqliteRecipeRepository(_db.Connections),
        Ingredients: new SqliteIngredientRepository(_db.Connections),
        Units: new SqliteUnitRepository(_db.Connections),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Configuration: new InMemoryConfigurationRepository());
}
