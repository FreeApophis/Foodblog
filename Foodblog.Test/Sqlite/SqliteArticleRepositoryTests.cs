namespace Foodblog.Test.Sqlite;

public sealed class SqliteArticleRepositoryTests : ArticleRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IArticleRepository CreateRepository() => new SqliteArticleRepository(_db.Connections);
}
