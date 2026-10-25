namespace FunctionalBlog.Test.Sqlite;

public sealed class SqlitePageRepositoryTests : PageRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IPageRepository CreateRepository() => new SqlitePageRepository(_db.Connections);
}
