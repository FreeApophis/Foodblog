using Dapper;

namespace Foodblog.Test.Sqlite;

public sealed class DatabaseMigratorTests
{
    [Fact]
    public async Task Migrate_accepts_a_journal_written_before_the_rename_to_Foodblog()
    {
        using var db = new SqliteTestBase();
        var connectionString = db.Connection.ConnectionString;
        await db.Connection.ExecuteAsync(
            "UPDATE SchemaVersions SET ScriptName = replace(ScriptName, 'Foodblog.', 'FunctionalBlog.')");

        DatabaseMigrator.Migrate(connectionString);

        var scriptNames = (await db.Connection.QueryAsync<string>("SELECT ScriptName FROM SchemaVersions")).ToList();
        Assert.Equal(scriptNames.Count, scriptNames.Distinct().Count());
        Assert.All(scriptNames, name => Assert.StartsWith("Foodblog.DataAccess.Sqlite.Migrations.", name));
    }
}
