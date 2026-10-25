using Dapper;
using Foodblog.DataAccess.Sqlite;

namespace Foodblog.DataAccess.Pages;

public sealed class SqlitePageRepository : IPageRepository
{
    private readonly SqliteConnectionFactory _connections;
    private readonly IdSequence _ids;

    public SqlitePageRepository(SqliteConnectionFactory connections)
    {
        _connections = connections;
        _ids = new IdSequence(connections, "pages");
    }

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

    public async ValueTask<PageId> NextId() =>
        new(await _ids.Next());

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
