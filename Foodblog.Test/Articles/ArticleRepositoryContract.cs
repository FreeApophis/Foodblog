namespace Foodblog.Test.Articles;

public abstract class ArticleRepositoryContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_article()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var article = AnArticle(id);

        await repo.Save(article);

        Assert.Equal(Option.Some(article), await repo.Find(id));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_id()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.Find(new ArticleId(987_654)));
    }

    [Fact]
    public async Task NextId_returns_an_id_that_does_not_yet_exist()
    {
        var repo = CreateRepository();

        var id = await repo.NextId();

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task NextId_returns_distinct_values_across_calls()
    {
        var repo = CreateRepository();

        var first = await repo.NextId();
        var second = await repo.NextId();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task All_returns_saved_articles_in_descending_PublishedAt_order()
    {
        var repo = CreateRepository();
        var older = AnArticle(await repo.NextId(), publishedAt: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var newer = AnArticle(await repo.NextId(), publishedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await repo.Save(older);
        await repo.Save(newer);

        var saved = (await repo.All())
            .Where(a => a.Id == older.Id || a.Id == newer.Id)
            .ToList();

        Assert.Equal(new[] { newer, older }, saved);
    }

    [Fact]
    public async Task Save_replaces_an_existing_article_with_the_same_id()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var original = AnArticle(id, title: "Original");
        var updated = AnArticle(id, title: "Aktualisiert");

        await repo.Save(original);
        await repo.Save(updated);

        Assert.Equal(Option.Some(updated), await repo.Find(id));
    }

    [Fact]
    public async Task Save_then_Find_preserves_the_cover_image_id()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var article = AnArticle(id) with { CoverImageId = Option.Some(new ImageId(42)) };

        await repo.Save(article);

        var found = FunctionalAssert.Some(await repo.Find(id));
        Assert.Equal(Option.Some(new ImageId(42)), found.CoverImageId);
    }

    [Fact]
    public async Task Delete_removes_the_article()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        await repo.Save(AnArticle(id));

        await repo.Delete(id);

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task Delete_is_idempotent_for_unknown_id()
    {
        var repo = CreateRepository();

        await repo.Delete(new ArticleId(987_654));
    }

    protected abstract IArticleRepository CreateRepository();

    private static Article AnArticle(
        ArticleId id,
        string title = "Titel",
        string text = "Text",
        DateTimeOffset? publishedAt = null) =>
        Article.Create(
            id,
            new ArticleTitle(title),
            new ArticleTeaser("Ein kurzer Teaser für den Artikel."),
            new ArticleText(text),
            new UserId(1),
            publishedAt ?? new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
}
