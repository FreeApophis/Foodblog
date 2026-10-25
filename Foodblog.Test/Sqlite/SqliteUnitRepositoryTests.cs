namespace Foodblog.Test.Sqlite;

public sealed class SqliteUnitRepositoryTests : UnitRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IUnitRepository CreateRepository() => new SqliteUnitRepository(_db.Connections);
}
