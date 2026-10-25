namespace FunctionalBlog.Test.Sqlite;

public sealed class SqliteConfigurationRepositoryTests : Test.Configuration.ConfigurationRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IConfigurationRepository Create() => new SqliteConfigurationRepository(_db.Connections);
}
