namespace FunctionalBlog.Test.Sqlite;

// The web app serves requests in parallel, so repositories are hit concurrently. These run many
// operations at once against one repository instance, the way the live pipeline does.
public sealed class SqliteConcurrencyTests : IDisposable
{
    private const int Parallelism = 200;

    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Concurrent_reads_do_not_fail()
    {
        var users = new SqliteUserRepository(_db.Connections);
        var recipes = new SqliteRecipeRepository(_db.Connections);

        await Task.WhenAll(Enumerable.Range(0, Parallelism).Select(i => Task.Run(async () =>
        {
            await users.FindById(new UserId(1));
            await recipes.Find(new RecipeId((i % 30) + 1));
        })));
    }
}
