using Dapper;
using FunctionalBlog.Application.Configuration;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteConfigurationRepository : IConfigurationRepository
{
    private readonly SqliteConnectionFactory _connections;

    public SqliteConfigurationRepository(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask<IReadOnlyDictionary<string, string>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<ConfigRow>("SELECT key AS Key, value AS Value FROM configuration");
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }

    public async ValueTask<Option<string>> Get(string key)
    {
        await using var connection = await _connections.OpenAsync();
        var value = await connection.QuerySingleOrDefaultAsync<string?>(
            "SELECT value FROM configuration WHERE key = @key", new { key });
        return Option.FromNullable(value);
    }

    public async ValueTask Set(string key, string value)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO configuration (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """,
            new { key, value });
    }

    private sealed record ConfigRow(string Key, string Value);
}
