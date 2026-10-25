using Dapper;
using FunctionalBlog.DataAccess.Sqlite;

namespace FunctionalBlog.DataAccess.Pages;

public sealed class SqlitePageRepository : IPageRepository
{
    private readonly SqliteConnectionFactory _connections;
    private int _nextId = -1;

    public SqlitePageRepository(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask<IReadOnlyList<Page>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<PageRow>(
            "SELECT id AS Id, title AS Title, content AS Content FROM pages ORDER BY id");
        return rows.Select(ToPage).ToList();
    }

    public async ValueTask<Option<Page>> Find(PageId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<PageRow>(
            "SELECT id AS Id, title AS Title, content AS Content FROM pages WHERE id = @id",
            new { id = id.Value });

        return Option.FromNullable(row).Select(ToPage);
    }

    public async ValueTask<PageId> NextId()
    {
        await using var connection = await _connections.OpenAsync();
        if (_nextId < 0)
        {
            _nextId = await connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(id), 0) FROM pages");
        }

        return new PageId(++_nextId);
    }

    public async ValueTask Save(Page page)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT OR REPLACE INTO pages (id, title, content)
            VALUES (@Id, @Title, @Content)
            """,
            new
            {
                Id = page.Id.Value,
                Title = page.Title.Value,
                Content = page.Content.Value,
            });
    }

    public async ValueTask Delete(PageId id)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM pages WHERE id = @id", new { id = id.Value });
    }

    private static Page ToPage(PageRow row) =>
        Page.Create(new PageId((int)row.Id), new PageTitle(row.Title), new PageContent(row.Content));

    private sealed record PageRow(long Id, string Title, string Content);
}
