namespace Foodblog.Test.Sqlite;

public sealed class SqliteSessionStoreTests : SessionStoreContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override ISessionStore CreateStore() => new SqliteSessionStore(_db.Connections);
}
