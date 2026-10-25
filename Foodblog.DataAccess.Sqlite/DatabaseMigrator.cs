using Dapper;
using DbUp;
using Microsoft.Data.Sqlite;

namespace Foodblog.DataAccess.Sqlite;

public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        DapperTypeHandlers.Register();
        RenameLegacyJournalEntries(connectionString);

        var result = DeployChanges.To
            .SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransaction()
            .WithVariablesDisabled()
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }

    // dbup journals scripts by embedded resource name, which carries the assembly's namespace.
    // Databases migrated before the FunctionalBlog → Foodblog rename would otherwise re-run every script.
    private static void RenameLegacyJournalEntries(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var hasJournal = connection.ExecuteScalar<long>(
            "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'SchemaVersions'") > 0;

        if (hasJournal)
        {
            connection.Execute(
                """
                UPDATE SchemaVersions
                SET ScriptName = 'Foodblog.' || substr(ScriptName, length('FunctionalBlog.') + 1)
                WHERE ScriptName LIKE 'FunctionalBlog.%'
                """);
        }
    }
}
