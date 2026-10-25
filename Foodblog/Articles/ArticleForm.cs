namespace Foodblog.Articles;

public static class ArticleForm
{
    public sealed record Valid(ArticleTitle Title, ArticleTeaser Teaser, ArticleText Text);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var title = request.Form.GetValueOrNone("title").GetOrElse(string.Empty).Trim();
        var teaser = request.Form.GetValueOrNone("teaser").GetOrElse(string.Empty).Trim();
        var text = request.Form.GetValueOrNone("text").GetOrElse(string.Empty).Trim();

        Func<ArticleTitle, ArticleTeaser, ArticleText, Valid> create =
            (t, te, tx) => new Valid(t, te, tx);

        return create
            .Apply(TryParseTitle(title), Combine)
            .Apply(TryParseTeaser(teaser), Combine)
            .Apply(TryParseText(text), Combine);
    }

    private static Validated<IReadOnlyList<string>, ArticleTitle> TryParseTitle(string raw) =>
        raw.Length >= 3
            ? Validated.Succeed<IReadOnlyList<string>, ArticleTitle>(new ArticleTitle(raw))
            : Validated.Fail<IReadOnlyList<string>, ArticleTitle>(["article.error.title_too_short"]);

    private static Validated<IReadOnlyList<string>, ArticleTeaser> TryParseTeaser(string raw) =>
        raw.Length >= 10
            ? Validated.Succeed<IReadOnlyList<string>, ArticleTeaser>(new ArticleTeaser(raw))
            : Validated.Fail<IReadOnlyList<string>, ArticleTeaser>(["article.error.teaser_too_short"]);

    private static Validated<IReadOnlyList<string>, ArticleText> TryParseText(string raw) =>
        raw.Length >= 10
            ? Validated.Succeed<IReadOnlyList<string>, ArticleText>(new ArticleText(raw))
            : Validated.Fail<IReadOnlyList<string>, ArticleText>(["article.error.text_too_short"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [..a, ..b];
}
