using DbUp;

namespace Foodblog.DataAccess.Sqlite;

public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        DapperTypeHandlers.Register();

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
}
