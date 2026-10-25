using Microsoft.Data.Sqlite;

namespace Foodblog.DataAccess.Sqlite;

// Hands out a fresh connection per repository operation. A SqliteConnection is not thread-safe, and
// the web app serves requests in parallel, so connections must never be shared between operations.
// Opening is cheap: Microsoft.Data.Sqlite pools the underlying handles (Pooling=True). Foreign keys
// are switched on per connection through the connection string; WAL is a property of the database
// file and is enabled once in Create. Concurrent writers wait for the lock (up to the command
// timeout) instead of failing.
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    private SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;

    public static SqliteConnectionFactory Create(string connectionString)
    {
        var pooled = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true, Pooling = true }.ToString();

        using var connection = new SqliteConnection(pooled);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();

        return new SqliteConnectionFactory(pooled);
    }

    // Closes this database's idle pooled connections (e.g. before deleting the file); connections in
    // use are closed when returned. Other databases' pools are untouched.
    public void ClearPool()
    {
        using var connection = new SqliteConnection(_connectionString);
        SqliteConnection.ClearPool(connection);
    }

    public async ValueTask<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
