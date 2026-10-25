using System.Data;
using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteSessionStore : ISessionStore
{
    private readonly IDbConnection _connection;

    public SqliteSessionStore(IDbConnection connection) => _connection = connection;

    public async ValueTask Save(Session session)
    {
        await _connection.ExecuteAsync(
            "INSERT OR REPLACE INTO sessions (token, user_id, expires_at) VALUES (@Token, @UserId, @ExpiresAt)",
            new { session.Token, UserId = session.UserId.Value, session.ExpiresAt });
    }

    public async ValueTask<Option<Session>> Find(string token)
    {
        var row = await _connection.QuerySingleOrDefaultAsync<SessionRow>(
            "SELECT token AS Token, user_id AS UserId, expires_at AS ExpiresAt FROM sessions WHERE token = @token",
            new { token });
        return row is null ? Option<Session>.None : Option.Some(new Session(row.Token, new UserId((int)row.UserId), row.ExpiresAt));
    }

    public async ValueTask Delete(string token)
    {
        await _connection.ExecuteAsync("DELETE FROM sessions WHERE token = @token", new { token });
    }

    public async ValueTask DeleteAllForUser(UserId userId)
    {
        await _connection.ExecuteAsync("DELETE FROM sessions WHERE user_id = @userId", new { userId = userId.Value });
    }

    private sealed record SessionRow(string Token, long UserId, DateTimeOffset ExpiresAt);
}
