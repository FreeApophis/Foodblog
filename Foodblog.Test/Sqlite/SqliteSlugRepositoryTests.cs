namespace Foodblog.Test.Sqlite;

public sealed class SqliteSlugRepositoryTests : Test.Slugs.SlugRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override ISlugRepository Create() => new SqliteSlugRepository(_db.Connections);
}
