namespace FunctionalBlog.Test.Sqlite;

// NextId is called by parallel requests on the one shared repository instance. Two callers must
// never get the same id: Save is an upsert, so a duplicate id silently overwrites the other row.
public sealed class SqliteIdSequenceTests : IDisposable
{
    private const int Parallelism = 200;

    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    public static TheoryData<string> Repositories =>
        ["articles", "images", "ingredients", "pages", "recipes", "roles", "units", "users"];

    [Theory]
    [MemberData(nameof(Repositories))]
    public async Task Concurrent_NextId_calls_hand_out_distinct_ids(string repository)
    {
        var nextId = NextIdOf(repository);

        var ids = await Task.WhenAll(Enumerable.Range(0, Parallelism).Select(_ => Task.Run(nextId)));

        Assert.Equal(Parallelism, ids.Distinct().Count());
    }

    [Fact]
    public async Task Concurrent_creates_are_all_persisted()
    {
        var users = new SqliteUserRepository(_db.Connections);
        var before = (await users.All()).Count;

        await Task.WhenAll(Enumerable.Range(0, Parallelism).Select(i => Task.Run(async () =>
        {
            var id = await users.NextId();
            await users.Save(User.Create(id, new Email($"user{i}@example.org"), new DisplayName($"User {i}"), "hash", ["Benutzer"], DateTimeOffset.UtcNow));
        })));

        Assert.Equal(before + Parallelism, (await users.All()).Count);
    }

    [Fact]
    public async Task NextId_continues_above_the_highest_stored_id()
    {
        var users = new SqliteUserRepository(_db.Connections);
        var highest = (await users.All()).Max(u => u.Id.Value);

        Assert.Equal(highest + 1, (await users.NextId()).Value);
    }

    private Func<Task<int>> NextIdOf(string repository) => repository switch
    {
        "articles" => Shared(new SqliteArticleRepository(_db.Connections), async r => (await r.NextId()).Value),
        "images" => Shared(new SqliteImageRepository(_db.Connections), async r => (await r.NextId()).Value),
        "ingredients" => Shared(new SqliteIngredientRepository(_db.Connections), async r => (await r.NextId()).Value),
        "pages" => Shared(new SqlitePageRepository(_db.Connections), async r => (await r.NextId()).Value),
        "recipes" => Shared(new SqliteRecipeRepository(_db.Connections), async r => (await r.NextId()).Value),
        "roles" => Shared(new SqliteRoleRepository(_db.Connections), async r => (await r.NextId()).Value),
        "units" => Shared(new SqliteUnitRepository(_db.Connections), async r => (await r.NextId()).Value),
        "users" => Shared(new SqliteUserRepository(_db.Connections), async r => (await r.NextId()).Value),
        _ => throw new ArgumentOutOfRangeException(nameof(repository)),
    };

    // One repository instance shared by all callers, like the app's single Env.
    private static Func<Task<int>> Shared<T>(T repository, Func<T, Task<int>> nextId) => () => nextId(repository);
}
