namespace Foodblog.Pages;

public static class PageForm
{
    public sealed record Valid(PageTitle Title, PageContent Content);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var title = request.Form.GetValueOrNone("title").GetOrElse(string.Empty).Trim();
        var content = request.Form.GetValueOrNone("content").GetOrElse(string.Empty).Trim();

        Func<PageTitle, PageContent, Valid> create = (t, c) => new Valid(t, c);

        return create
            .Apply(TryParseTitle(title), Combine)
            .Apply(TryParseContent(content), Combine);
    }

    private static Validated<IReadOnlyList<string>, PageTitle> TryParseTitle(string raw) =>
        raw.Length >= 3
            ? Validated.Succeed<IReadOnlyList<string>, PageTitle>(new PageTitle(raw))
            : Validated.Fail<IReadOnlyList<string>, PageTitle>(["page.error.title_too_short"]);

    private static Validated<IReadOnlyList<string>, PageContent> TryParseContent(string raw) =>
        raw.Length >= 1
            ? Validated.Succeed<IReadOnlyList<string>, PageContent>(new PageContent(raw))
            : Validated.Fail<IReadOnlyList<string>, PageContent>(["page.error.content_required"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
