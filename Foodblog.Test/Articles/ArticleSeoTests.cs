namespace Foodblog.Test.Articles;

public class ArticleSeoTests
{
    [Fact]
    public void Build_sets_share_metadata_from_the_article()
    {
        var meta = ArticleSeo.Build(Sample(Option.Some(new ImageId(5))), "Anna", "https://foodblog.ch");

        Assert.Equal("article", meta.Type);
        Assert.Equal("https://foodblog.ch/articles/9", meta.Url);
        Assert.Equal("https://foodblog.ch/images/5", meta.ImageUrl);
        Assert.Contains("Leichte Gerichte", meta.Description);
    }

    [Fact]
    public void Build_emits_blogposting_json_ld()
    {
        var json = ArticleSeo.Build(Sample(Option.Some(new ImageId(5))), "Anna", "https://foodblog.ch").HeadExtra;

        Assert.Contains("<script type=\"application/ld+json\">", json);
        Assert.Contains("\"@type\":\"BlogPosting\"", json);
        Assert.Contains("\"headline\":\"Sommerk", json);
        Assert.Contains("\"datePublished\":\"2026-06-24T08:00:00Z\"", json);
        Assert.Contains("Anna", json);
        Assert.Contains("https://foodblog.ch/images/5", json);
    }

    [Fact]
    public void Build_uses_the_slug_for_canonical_urls_when_supplied()
    {
        var meta = ArticleSeo.Build(Sample(), "Anna", "https://foodblog.ch", "sommerkueche");

        Assert.Equal("https://foodblog.ch/articles/sommerkueche", meta.Url);
        Assert.Contains("\"url\":\"https://foodblog.ch/articles/sommerkueche\"", meta.HeadExtra);
    }

    [Fact]
    public void Build_omits_image_when_there_is_no_cover()
    {
        var meta = ArticleSeo.Build(Sample(), "Anna", "https://foodblog.ch");

        Assert.Equal(string.Empty, meta.ImageUrl);
        Assert.DoesNotContain("\"image\"", meta.HeadExtra);
    }

    private static Article Sample(Option<ImageId> cover = default) => Article.Create(
        new ArticleId(9),
        new ArticleTitle("Sommerküche"),
        new ArticleTeaser("Leichte Gerichte für heiße Tage."),
        new ArticleText("Voller [b]Text[/b] hier."),
        new UserId(1),
        new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(2)),
        cover);
}
