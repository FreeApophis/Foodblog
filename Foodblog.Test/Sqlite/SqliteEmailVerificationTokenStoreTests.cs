namespace Foodblog.Test.Sqlite;

public sealed class SqliteEmailVerificationTokenStoreTests : Test.Identity.EmailVerificationTokenStoreContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IEmailVerificationTokenStore CreateStore() => new SqliteEmailVerificationTokenStore(_db.Connections);
}
