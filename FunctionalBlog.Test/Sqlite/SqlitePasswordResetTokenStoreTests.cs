namespace FunctionalBlog.Test.Sqlite;

public sealed class SqlitePasswordResetTokenStoreTests : PasswordResetTokenStoreContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IPasswordResetTokenStore CreateStore() => new SqlitePasswordResetTokenStore(_db.Connections);
}
