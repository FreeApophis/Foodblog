namespace Foodblog.Test.Sqlite;

// The 0002_seed migration ships a single admin@blog.de account without a password, owning all seeded
// content. The seeder must give it a one-time password on the first startup.
public sealed class SeederSqliteTests : IDisposable
{
    private const string BootstrapPasswordMarker = "bootstrap-admin-password:";

    private readonly SqliteTestBase _db = new();
    private readonly TestLog _log = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SeedAsync_gives_the_migration_seeded_admin_a_one_time_password()
    {
        var env = BuildEnv();
        var before = FunctionalAssert.Some(await env.Users.FindByEmail(new Email("admin@blog.de")));

        await Seeder.SeedAsync(env);

        var after = FunctionalAssert.Some(await env.Users.FindByEmail(new Email("admin@blog.de")));
        var line = Assert.Single(_log.Messages, m => m.Contains(BootstrapPasswordMarker));
        var password = line[(line.IndexOf(BootstrapPasswordMarker, StringComparison.Ordinal) + BootstrapPasswordMarker.Length)..].Trim().Split(' ')[0];
        Assert.True(env.PasswordHasher.Verify(password, after!.PasswordHash));
        Assert.Equal(before!.Id, after.Id);
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
        Log: _log,
        CurrentUser: Guest.Instance,
        Recipes: new SqliteRecipeRepository(_db.Connections),
        Ingredients: new SqliteIngredientRepository(_db.Connections),
        Units: new SqliteUnitRepository(_db.Connections),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Configuration: new InMemoryConfigurationRepository());
}
