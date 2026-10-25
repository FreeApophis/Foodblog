using Dapper;

namespace Foodblog.DataAccess.Sqlite;

public sealed class SqliteEmailVerificationTokenStore : IEmailVerificationTokenStore
{
    private readonly SqliteConnectionFactory _connections;

    public SqliteEmailVerificationTokenStore(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask Save(EmailVerificationToken token)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            "INSERT OR REPLACE INTO email_verification_tokens (token, user_id, expires_at, consumed) VALUES (@Token, @UserId, @ExpiresAt, @Consumed)",
            new { token.Token, UserId = token.UserId.Value, token.ExpiresAt, Consumed = token.Consumed ? 1 : 0 });
    }

    public async ValueTask<Option<EmailVerificationToken>> Find(string token)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<TokenRow>(
            "SELECT token AS Token, user_id AS UserId, expires_at AS ExpiresAt, consumed AS Consumed FROM email_verification_tokens WHERE token = @token",
            new { token });
        return Option.FromNullable(row).Select(r => new EmailVerificationToken(r.Token, new UserId((int)r.UserId), r.ExpiresAt, r.Consumed != 0L));
    }

    public async ValueTask Consume(string token)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            "UPDATE email_verification_tokens SET consumed = 1 WHERE token = @token",
            new { token });
    }

    private sealed record TokenRow(string Token, long UserId, DateTimeOffset ExpiresAt, long Consumed);
}
