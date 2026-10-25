using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly SqliteConnectionFactory _connections;
    private int _nextId = -1;

    public SqliteUserRepository(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask<IReadOnlyList<User>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = (await connection.QueryAsync<UserRow>(
            "SELECT id AS Id, email AS Email, display_name AS DisplayName, password_hash AS PasswordHash, created_at AS CreatedAt, avatar_image_id AS AvatarImageId, email_verified AS EmailVerified FROM users")).ToList();
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(r => r.Id).ToList();
        var roleLookup = (await connection.QueryAsync<RoleNameRow>(
            "SELECT user_id AS UserId, role_name AS RoleName FROM user_roles WHERE user_id IN @ids",
            new { ids })).ToLookup(r => r.UserId, r => r.RoleName);

        return rows.Select(r => ToUser(r, roleLookup[r.Id].ToList())).ToList();
    }

    public async ValueTask<Option<User>> FindById(UserId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id AS Id, email AS Email, display_name AS DisplayName, password_hash AS PasswordHash, created_at AS CreatedAt, avatar_image_id AS AvatarImageId, email_verified AS EmailVerified FROM users WHERE id = @id",
            new { id = id.Value });
        if (row is null)
        {
            return Option<User>.None;
        }

        var roles = (await connection.QueryAsync<string>(
            "SELECT role_name FROM user_roles WHERE user_id = @id",
            new { id = id.Value })).ToList();
        return Option.Some(ToUser(row, roles));
    }

    public async ValueTask<Option<User>> FindByEmail(Email email)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id AS Id, email AS Email, display_name AS DisplayName, password_hash AS PasswordHash, created_at AS CreatedAt, avatar_image_id AS AvatarImageId, email_verified AS EmailVerified FROM users WHERE email = @email",
            new { email = email.Value });
        if (row is null)
        {
            return Option<User>.None;
        }

        var roles = (await connection.QueryAsync<string>(
            "SELECT role_name FROM user_roles WHERE user_id = @id",
            new { id = row.Id })).ToList();
        return Option.Some(ToUser(row, roles));
    }

    public async ValueTask<UserId> NextId()
    {
        await using var connection = await _connections.OpenAsync();
        if (_nextId < 0)
        {
            _nextId = await connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(id), 0) FROM users");
        }

        return new UserId(++_nextId);
    }

    public async ValueTask Save(User user)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO users (id, email, display_name, password_hash, created_at, avatar_image_id, email_verified)
            VALUES (@Id, @Email, @DisplayName, @PasswordHash, @CreatedAt, @AvatarImageId, @EmailVerified)
            ON CONFLICT(id) DO UPDATE SET
                email           = excluded.email,
                display_name    = excluded.display_name,
                password_hash   = excluded.password_hash,
                created_at      = excluded.created_at,
                avatar_image_id = excluded.avatar_image_id,
                email_verified  = excluded.email_verified
            """,
            new
            {
                Id = user.Id.Value,
                Email = user.Email.Value,
                DisplayName = user.DisplayName.Value,
                user.PasswordHash,
                user.CreatedAt,
                AvatarImageId = user.AvatarImageId.Match(none: (int?)null, some: imageId => imageId.Value),
                EmailVerified = user.EmailVerified ? 1 : 0,
            });

        await connection.ExecuteAsync(
            "DELETE FROM user_roles WHERE user_id = @id",
            new { id = user.Id.Value });

        if (user.RoleNames.Count > 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO user_roles (user_id, role_name) VALUES (@UserId, @RoleName)",
                user.RoleNames.Select(r => new { UserId = user.Id.Value, RoleName = r }));
        }
    }

    private static User ToUser(UserRow row, IReadOnlyList<string> roles) =>
        User.Create(
            new UserId((int)row.Id),
            new Email(row.Email),
            new DisplayName(row.DisplayName),
            row.PasswordHash,
            roles,
            row.CreatedAt,
            row.AvatarImageId is { } avatarId ? Option.Some(new ImageId((int)avatarId)) : Option<ImageId>.None,
            row.EmailVerified != 0);

    private sealed record UserRow(long Id, string Email, string DisplayName, string PasswordHash, DateTimeOffset CreatedAt, long? AvatarImageId, long EmailVerified);

    private sealed record RoleNameRow(long UserId, string RoleName);
}
