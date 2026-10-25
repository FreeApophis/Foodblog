namespace Foodblog.Admin;

public static class AdminSearchViews
{
    // The full /admin/search maintenance page: breadcrumb, head, and the status panel.
    public static string Status(SearchIndexStatus status, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("admin.search.title")));

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("admin.dashboard.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(t("admin.search.title"))}</h1></div>
                <p class="page-head-blurb">{Html.Encode(t("admin.search.blurb"))}</p>
            </div>
            """);

        var body = breadcrumb + head + Html.Raw(Panel(status, rebuilt: false, ctx));
        return Layout.Page(t("admin.search.title"), body, ctx);
    }

    // The status panel: per-type document counts, last-rebuilt time, and the rebuild button. It is
    // its own htmx swap target so the rebuild POST can refresh it in place. When `rebuilt` is true a
    // confirmation note is shown (after a successful rebuild).
    public static string Panel(SearchIndexStatus status, bool rebuilt, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var note = rebuilt
            ? $"""<div class="eyebrow eyebrow-accent">{Html.Encode(t("admin.search.rebuilt"))}</div>"""
            : string.Empty;

        var lastRebuilt = status.LastRebuilt is [var when]
            ? $"{when.UtcDateTime:yyyy-MM-dd HH:mm} UTC"
            : t("admin.search.never");

        string Row(string labelKey, int count) =>
            $"""<tr><td>{Html.Encode(t(labelKey))}</td><td class="num">{count}</td></tr>""";

        return $"""
            <section id="search-index-status" class="card search-status">
                {note}
                <table>
                    <tbody>
                        {Row("admin.search.field.articles", status.Articles)}
                        {Row("admin.search.field.recipes", status.Recipes)}
                        {Row("admin.search.field.ingredients", status.Ingredients)}
                        {Row("admin.search.field.pages", status.Pages)}
                        <tr><td><strong>{Html.Encode(t("admin.search.field.total"))}</strong></td><td class="num"><strong>{status.Total}</strong></td></tr>
                    </tbody>
                </table>
                <p class="muted">{Html.Encode(t("admin.search.last_rebuilt"))}: {Html.Encode(lastRebuilt)}</p>
                <form method="post" action="/admin/search/rebuild">
                    <input type="hidden" name="_csrf" value="{Html.Encode(csrfToken)}" />
                    <button type="submit"
                            hx-post="/admin/search/rebuild" hx-target="#search-index-status" hx-swap="outerHTML"
                            hx-disabled-elt="this">
                        <span class="btn-label">{Html.Encode(t("admin.search.rebuild"))}</span>
                        <span class="btn-busy"><span class="spinner" aria-hidden="true"></span>{Html.Encode(t("admin.search.rebuilding"))}</span>
                    </button>
                </form>
            </section>
            """;
    }
}
