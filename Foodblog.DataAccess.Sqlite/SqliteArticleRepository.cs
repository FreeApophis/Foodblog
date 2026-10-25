using System.Data;
using Dapper;

namespace Foodblog.DataAccess.Sqlite;

public sealed class SqliteArticleRepository : IArticleRepository
{
    private readonly SqliteConnectionFactory _connections;
    private readonly IdSequence _ids;

    public SqliteArticleRepository(SqliteConnectionFactory connections)
    {
        _connections = connections;
        _ids = new IdSequence(connections, "articles");
    }

    public async ValueTask<IReadOnlyList<Article>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<ArticleRow>(
            "SELECT id AS Id, title AS Title, teaser AS Teaser, text AS Text, author_id AS AuthorId, published_at AS PublishedAt, cover_image_id AS CoverImageId FROM articles ORDER BY published_at DESC");
        return rows.Select(ToArticle).ToList();
    }

    public async ValueTask<IReadOnlyList<Article>> FindByTag(string slug)
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<ArticleRow>(
            "SELECT id AS Id, title AS Title, teaser AS Teaser, text AS Text, author_id AS AuthorId, published_at AS PublishedAt, cover_image_id AS CoverImageId " +
            "FROM articles WHERE taggable_id IN " +
            "(SELECT tg.taggable_id FROM taggings tg " +
            "JOIN slugs s ON s.entity_type = 'tag' AND s.entity_id = tg.tag_id WHERE s.slug = @slug) " +
            "ORDER BY published_at DESC",
            new { slug });
        return rows.Select(ToArticle).ToList();
    }

    public async ValueTask<Option<Article>> Find(ArticleId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<ArticleRow>(
            "SELECT id AS Id, title AS Title, teaser AS Teaser, text AS Text, author_id AS AuthorId, published_at AS PublishedAt, cover_image_id AS CoverImageId FROM articles WHERE id = @id",
            new { id = id.Value });

        return Option.FromNullable(row).Select(ToArticle);
    }

    public async ValueTask<ArticleId> NextId() =>
        new(await _ids.Next());

    public async ValueTask Save(Article article)
    {
        await using var connection = await _connections.OpenAsync();
        using var transaction = connection.BeginTransaction();

        // Reuse the article's existing taggable on update, mint a fresh one on insert.
        var taggableId = await EnsureTaggable(connection, article.Id.Value, transaction);

        await connection.ExecuteAsync(
            """
            INSERT OR REPLACE INTO articles (id, title, teaser, text, author_id, published_at, cover_image_id, taggable_id)
            VALUES (@Id, @Title, @Teaser, @Text, @AuthorId, @PublishedAt, @CoverImageId, @TaggableId)
            """,
            new
            {
                Id = article.Id.Value,
                Title = article.Title.Value,
                Teaser = article.Teaser.Value,
                Text = article.Text.Value,
                AuthorId = article.AuthorId.Value,
                article.PublishedAt,
                CoverImageId = article.CoverImageId.Match(none: (int?)null, some: imageId => imageId.Value),
                TaggableId = taggableId,
            },
            transaction);

        transaction.Commit();
    }

    public async ValueTask Delete(ArticleId id)
    {
        await using var connection = await _connections.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var taggableId = await connection.ExecuteScalarAsync<long?>(
            "SELECT taggable_id FROM articles WHERE id = @id", new { id = id.Value }, transaction);

        await connection.ExecuteAsync("DELETE FROM articles WHERE id = @id", new { id = id.Value }, transaction);

        // Removing the owned taggable cascades to its taggings.
        if (taggableId is { } owned)
        {
            await connection.ExecuteAsync(
                "DELETE FROM taggables WHERE id = @owned", new { owned }, transaction);
        }

        transaction.Commit();
    }

    // Returns the article's taggable id, minting a new taggable when the article is new.
    private async Task<long> EnsureTaggable(IDbConnection connection, int articleId, IDbTransaction transaction)
    {
        var existing = await connection.ExecuteScalarAsync<long?>(
            "SELECT taggable_id FROM articles WHERE id = @id", new { id = articleId }, transaction);

        if (existing is { } taggableId)
        {
            return taggableId;
        }

        await connection.ExecuteAsync("INSERT INTO taggables DEFAULT VALUES", transaction: transaction);
        return await connection.ExecuteScalarAsync<long>("SELECT last_insert_rowid()", transaction: transaction);
    }

    private static Article ToArticle(ArticleRow row) =>
        Article.Create(
            new ArticleId((int)row.Id),
            new ArticleTitle(row.Title),
            new ArticleTeaser(row.Teaser),
            new ArticleText(row.Text),
            new UserId((int)row.AuthorId),
            row.PublishedAt,
            row.CoverImageId is { } coverId ? Option.Some(new ImageId((int)coverId)) : Option<ImageId>.None);

    private sealed record ArticleRow(long Id, string Title, string Teaser, string Text, long AuthorId, DateTimeOffset PublishedAt, long? CoverImageId);
}
