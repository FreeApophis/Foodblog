namespace FunctionalBlog.Test.Sqlite;

public sealed class SqliteRecipeRepositoryTests : RecipeRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    protected override IRecipeRepository CreateRepository() => new SqliteRecipeRepository(_db.Connections);
}
