namespace Foodblog.Test.Articles;

public class ArticleTests
{
    [Fact]
    public void Create_returns_an_article_carrying_the_supplied_values()
    {
        var id = new ArticleId(42);
        var title = new ArticleTitle("Hallo Welt");
        var teaser = new ArticleTeaser("Eine kurze Zusammenfassung des Artikels.");
        var text = new ArticleText("Erster Beitrag.");
        var authorId = new UserId(1);
        var publishedAt = new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero);

        var article = Article.Create(id, title, teaser, text, authorId, publishedAt);

        Assert.Equal(new Article(id, title, teaser, text, authorId, publishedAt, Option<ImageId>.None), article);
    }
}
