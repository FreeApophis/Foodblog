using Dapper;

namespace Foodblog.DataAccess.Sqlite;

public sealed class SqliteTagRepository : ITagRepository
{
    private readonly SqliteConnectionFactory _connections;

    public SqliteTagRepository(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask<Option<Tag>> FindBySlug(string slug)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<TagRow>(
            "SELECT s.slug AS Slug, t.name AS Name " +
            "FROM slugs s JOIN tags t ON t.id = s.entity_id " +
            "WHERE s.entity_type = 'tag' AND s.slug = @slug",
            new { slug });

        return Option.FromNullable(row).Select(r => new Tag(r.Slug, r.Name));
    }

    public async ValueTask<IReadOnlyList<TagEntry>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<TagEntryRow>("SELECT id AS Id, name AS Name FROM tags");
        return rows.Select(r => new TagEntry((int)r.Id, r.Name)).ToList();
    }

    public async ValueTask<Option<int>> FindIdByName(string name)
    {
        await using var connection = await _connections.OpenAsync();
        var id = await connection.ExecuteScalarAsync<long?>(
            "SELECT id FROM tags WHERE name = @name COLLATE NOCASE ORDER BY id LIMIT 1",
            new { name });

        return id is { } value ? Option.Some((int)value) : Option<int>.None;
    }

    private sealed record TagRow(string Slug, string Name);

    private sealed record TagEntryRow(long Id, string Name);
}
