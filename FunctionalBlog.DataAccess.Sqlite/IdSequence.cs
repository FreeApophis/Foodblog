using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

// Hands out ids above a table's highest stored id. Repositories are shared by parallel requests,
// so the starting point is loaded exactly once (thread-safe Lazy) and every id after it comes from
// an atomic increment: no two callers ever get the same id. Ids are only handed out by this
// process, which is the database's only writer.
public sealed class IdSequence
{
    private readonly Lazy<Task<int>> _highestStoredId;
    private int _handedOut;

    public IdSequence(SqliteConnectionFactory connections, string table) =>
        _highestStoredId = new Lazy<Task<int>>(async () =>
        {
            await using var connection = await connections.OpenAsync();
            return await connection.ExecuteScalarAsync<int>($"SELECT COALESCE(MAX(id), 0) FROM {table}");
        });

    public async ValueTask<int> Next() =>
        await _highestStoredId.Value + Interlocked.Increment(ref _handedOut);
}
