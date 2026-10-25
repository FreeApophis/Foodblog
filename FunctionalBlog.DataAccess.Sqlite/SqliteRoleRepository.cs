using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteRoleRepository : IRoleRepository
{
    private readonly SqliteConnectionFactory _connections;
    private readonly IdSequence _ids;

    public SqliteRoleRepository(SqliteConnectionFactory connections)
    {
        _connections = connections;
        _ids = new IdSequence(connections, "roles");
    }

    public async ValueTask<IReadOnlyList<Role>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = (await connection.QueryAsync<RoleRow>(
            "SELECT id AS Id, name AS Name FROM roles")).ToList();
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(r => r.Id).ToList();
        var ruleLookup = (await connection.QueryAsync<RuleRow>(
            "SELECT role_id AS RoleId, action_name AS ActionName, resource_key AS ResourceKey FROM permission_rules WHERE role_id IN @ids",
            new { ids })).ToLookup(r => r.RoleId);

        return rows.Select(r => ToRole(r, ruleLookup[r.Id].ToList())).ToList();
    }

    public async ValueTask<Option<Role>> FindById(RoleId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<RoleRow>(
            "SELECT id AS Id, name AS Name FROM roles WHERE id = @id",
            new { id = id.Value });
        if (row is null)
        {
            return Option<Role>.None;
        }

        var rules = (await connection.QueryAsync<RuleRow>(
            "SELECT role_id AS RoleId, action_name AS ActionName, resource_key AS ResourceKey FROM permission_rules WHERE role_id = @id",
            new { id = id.Value })).ToList();
        return Option.Some(ToRole(row, rules));
    }

    public async ValueTask<Option<Role>> FindByName(string name)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<RoleRow>(
            "SELECT id AS Id, name AS Name FROM roles WHERE name = @name",
            new { name });
        if (row is null)
        {
            return Option<Role>.None;
        }

        var rules = (await connection.QueryAsync<RuleRow>(
            "SELECT role_id AS RoleId, action_name AS ActionName, resource_key AS ResourceKey FROM permission_rules WHERE role_id = @id",
            new { id = row.Id })).ToList();
        return Option.Some(ToRole(row, rules));
    }

    public async ValueTask<RoleId> NextId() =>
        new(await _ids.Next());

    public async ValueTask Save(Role role)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO roles (id, name)
            VALUES (@Id, @Name)
            ON CONFLICT(id) DO UPDATE SET name = excluded.name
            """,
            new { Id = role.Id.Value, role.Name });

        await connection.ExecuteAsync(
            "DELETE FROM permission_rules WHERE role_id = @id",
            new { id = role.Id.Value });

        if (role.Rules.Count > 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO permission_rules (role_id, action_name, resource_key) VALUES (@RoleId, @ActionName, @ResourceKey)",
                role.Rules.Select(r => new { RoleId = role.Id.Value, r.ActionName, r.ResourceKey }));
        }
    }

    public async ValueTask Delete(RoleId id)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM roles WHERE id = @id", new { id = id.Value });
    }

    private static Role ToRole(RoleRow row, IReadOnlyList<RuleRow> rules)
    {
        var role = Role.Create(new RoleId((int)row.Id), row.Name);
        return rules.Aggregate(role, (r, rule) => r.AddRule(new PermissionRule(rule.ActionName, rule.ResourceKey)));
    }

    private sealed record RoleRow(long Id, string Name);

    private sealed record RuleRow(long RoleId, string ActionName, string ResourceKey);
}
