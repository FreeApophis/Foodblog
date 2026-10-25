namespace FunctionalBlog.Test.Sqlite;

public sealed class SqliteIngredientRepositoryTests : IngredientRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IIngredientRepository CreateRepository() => new SqliteIngredientRepository(_db.Connections);
}
