namespace Foodblog.Test.Articles;

public sealed class ArticleFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(Decode("Guter Titel", "Ein ausreichend langer Teaser.", "Genug Text hier"));

        Assert.Equal(new ArticleTitle("Guter Titel"), form.Title);
        Assert.Equal(new ArticleTeaser("Ein ausreichend langer Teaser."), form.Teaser);
        Assert.Equal(new ArticleText("Genug Text hier"), form.Text);
    }

    [Fact]
    public void Title_shorter_than_3_characters_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("Ti", "Ein ausreichend langer Teaser.", "Genug Text hier"));

        Assert.Contains("article.error.title_too_short", errors);
    }

    [Fact]
    public void Teaser_shorter_than_10_characters_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("Guter Titel", "Zu kurz", "Genug Text hier"));

        Assert.Contains("article.error.teaser_too_short", errors);
    }

    [Fact]
    public void Text_shorter_than_10_characters_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("Guter Titel", "Ein ausreichend langer Teaser.", "Zu kurz"));

        Assert.Contains("article.error.text_too_short", errors);
    }

    [Fact]
    public void All_invalid_fields_accumulate_all_three_errors()
    {
        var errors = ValidatedAssert.IsFailure(Decode("Ti", "Zu", "Ku"));

        Assert.Contains("article.error.title_too_short", errors);
        Assert.Contains("article.error.teaser_too_short", errors);
        Assert.Contains("article.error.text_too_short", errors);
        Assert.Equal(3, errors.Count);
    }

    private static Validated<IReadOnlyList<string>, ArticleForm.Valid> Decode(string title, string teaser, string text) =>
        ArticleForm.Decode(new Request(
            HttpMethod.Post,
            "/articles",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string> { ["title"] = title, ["teaser"] = teaser, ["text"] = text },
            new Dictionary<string, string>()));
}
