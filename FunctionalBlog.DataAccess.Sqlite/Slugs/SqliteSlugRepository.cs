using Dapper;
using FunctionalBlog.Application.Slugs;
using FunctionalBlog.DataAccess.Sqlite;
using FunctionalBlog.Domain.Slugs;

namespace FunctionalBlog.DataAccess.Slugs;

public sealed class SqliteSlugRepository : ISlugRepository
{
    private readonly SqliteConnectionFactory _connections;

    public SqliteSlugRepository(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask<Option<SlugTarget>> FindTarget(string slug)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<TargetRow>(
            "SELECT entity_type AS EntityType, entity_id AS EntityId FROM slugs WHERE slug = @slug",
            new { slug });

        return row is null ? Option<SlugTarget>.None : Option.Some(new SlugTarget(row.EntityType, (int)row.EntityId));
    }

    public async ValueTask<Option<string>> FindSlug(string entityType, int entityId)
    {
        await using var connection = await _connections.OpenAsync();
        var slug = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT slug FROM slugs WHERE entity_type = @entityType AND entity_id = @entityId",
            new { entityType, entityId });

        return Option.FromNullable(slug);
    }

    public async ValueTask<IReadOnlyDictionary<int, string>> SlugsFor(string entityType)
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<SlugRow>(
            "SELECT entity_id AS EntityId, slug AS Slug FROM slugs WHERE entity_type = @entityType",
            new { entityType });

        return rows.ToDictionary(r => (int)r.EntityId, r => r.Slug);
    }

    public async ValueTask Upsert(string entityType, int entityId, string slug)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            "DELETE FROM slugs WHERE entity_type = @entityType AND entity_id = @entityId",
            new { entityType, entityId });

        await connection.ExecuteAsync(
            "INSERT INTO slugs (slug, entity_type, entity_id) VALUES (@slug, @entityType, @entityId)",
            new { slug, entityType, entityId });
    }

    private sealed record TargetRow(string EntityType, long EntityId);

    private sealed record SlugRow(long EntityId, string Slug);
}
