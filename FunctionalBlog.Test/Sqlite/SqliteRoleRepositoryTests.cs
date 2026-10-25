namespace FunctionalBlog.Test.Sqlite;

public sealed class SqliteRoleRepositoryTests : RoleRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IRoleRepository CreateRepository() => new SqliteRoleRepository(_db.Connections);
}
