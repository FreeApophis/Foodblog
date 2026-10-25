namespace FunctionalBlog.Test.Sqlite;

public sealed class SqliteImageRepositoryTests : ImageRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IImageRepository CreateRepository() => new SqliteImageRepository(_db.Connections);
}
