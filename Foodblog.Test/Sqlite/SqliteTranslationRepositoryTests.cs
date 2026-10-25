namespace Foodblog.Test.Sqlite;

public sealed class SqliteTranslationRepositoryTests : TranslationRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override ITranslationRepository CreateRepository() => new SqliteTranslationRepository(_db.Connections);
}
