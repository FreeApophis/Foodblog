using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteUnitRepository : IUnitRepository
{
    private readonly SqliteConnectionFactory _connections;
    private readonly IdSequence _ids;

    public SqliteUnitRepository(SqliteConnectionFactory connections)
    {
        _connections = connections;
        _ids = new IdSequence(connections, "units");
    }

    public async ValueTask<IReadOnlyList<Unit>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<UnitRow>(
            "SELECT id AS Id, category AS Category, unit_factor AS Factor, name_key AS NameKey, abbreviation_key AS AbbreviationKey FROM units ORDER BY id");
        return rows.Select(ToUnit).ToList();
    }

    public async ValueTask<Option<Unit>> Find(UnitId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<UnitRow>(
            "SELECT id AS Id, category AS Category, unit_factor AS Factor, name_key AS NameKey, abbreviation_key AS AbbreviationKey FROM units WHERE id = @id",
            new { id = id.Value });

        return Option.FromNullable(row).Select(ToUnit);
    }

    public async ValueTask<UnitId> NextId() =>
        new(await _ids.Next());

    public async ValueTask Save(Unit unit)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT OR REPLACE INTO units (id, category, unit_factor, name_key, abbreviation_key)
            VALUES (@Id, @Category, @Factor, @NameKey, @AbbreviationKey)
            """,
            new
            {
                Id = unit.Id.Value,
                Category = (int)unit.Category,
                unit.Factor,
                unit.NameKey,
                unit.AbbreviationKey,
            });
    }

    public async ValueTask Delete(UnitId id)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM units WHERE id = @id", new { id = id.Value });
    }

    private static Unit ToUnit(UnitRow row) =>
        new(new UnitId((int)row.Id), row.NameKey, row.AbbreviationKey, (UnitCategory)row.Category, row.Factor);

    private sealed record UnitRow(long Id, long Category, decimal Factor, string NameKey, string AbbreviationKey);
}
