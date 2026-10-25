namespace Foodblog.Search;

public static class SearchViews
{
    public static string Empty(ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var body =
            Html.H1(t("search.title")) +
            SearchForm(string.Empty, t) +
            Html.P(Html.Text(t("search.prompt")));

        return Layout.Page(t("search.title"), body, ctx);
    }

    public static string Results(
        string query,
        IReadOnlyList<SearchResult> results,
        IReadOnlyList<string> suggestions,
        ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var header = results.Count == 0
            ? Html.P(Html.Text(t("search.no_results")))
            : Html.P(Html.Text($"{results.Count} {t("search.results_for")} „{query}“"));

        var suggestionHtml = suggestions.Count > 0
            ? Html.P(Html.Text(t("search.did_you_mean") + " ") +
                HtmlString.Concat(suggestions.Select((s, i) =>
                    (i > 0 ? Html.Raw(", ") : HtmlString.Empty) +
                    Html.Link($"/search?q={Uri.EscapeDataString(s)}", s))))
            : HtmlString.Empty;

        var resultList = results.Count == 0
            ? HtmlString.Empty
            : Html.Ul(results.Select(r => ResultItem(r, ctx)));

        var body = Html.H1(t("search.title")) + header + suggestionHtml + resultList;

        return Layout.Page(t("search.title"), body, ctx);
    }

    // Inner HTML for the nav quicksearch dropdown: matches grouped by category, in the order the
    // quicksearch returns them (tags, articles, recipes, ingredients). Every match links to its page.
    public static string QuickResults(IReadOnlyList<QuickSearchHit> hits, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        if (hits.Count == 0)
        {
            return $"""<div class="quicksearch-panel"><div class="quicksearch-empty">{Html.Encode(t("search.no_results"))}</div></div>""";
        }

        var groups = hits
            .GroupBy(hit => hit.Category)
            .Select(group => QuickGroup(group.Key, group.ToList(), ctx));

        return $"""<div class="quicksearch-panel">{string.Concat(groups)}</div>""";
    }

    private static string QuickGroup(string category, IReadOnlyList<QuickSearchHit> hits, ViewContext ctx)
    {
        var heading = $"""<div class="quicksearch-cat">{Html.Encode(ctx.T($"search.type.{category}"))}</div>""";
        return $"""<div class="quicksearch-group">{heading}{string.Concat(hits.Select(hit => QuickItem(hit, ctx)))}</div>""";
    }

    private static string QuickItem(QuickSearchHit hit, ViewContext ctx) =>
        QuickUrl(hit, ctx) is { } url
            ? $"""<a class="quicksearch-item" href="{Html.Encode(url)}">{Html.Encode(hit.Label)}</a>"""
            : $"""<span class="quicksearch-item is-static">{Html.Encode(hit.Label)}</span>""";

    private static string? QuickUrl(QuickSearchHit hit, ViewContext ctx) => hit.Category switch
    {
        "tag" when hit.Slug is { } slug => $"/tag/{Uri.EscapeDataString(slug)}",
        "article" when hit.Id is { } id => ctx.Url(SlugEntityType.Article, id),
        "recipe" when hit.Id is { } id => ctx.Url(SlugEntityType.Recipe, id),
        "ingredient" when hit.Id is { } id => ctx.Url(SlugEntityType.Ingredient, id),
        _ => null,
    };

    private static HtmlString SearchForm(string query, Translate t) =>
        Html.Raw($"""<form action="/search" method="get" class="search-form">""") +
        Html.Input("q", query) +
        Html.Button(t("search.submit")) +
        Html.Raw("</form>");

    private static HtmlString ResultItem(SearchResult result, ViewContext ctx)
    {
        var t = ctx.T;
        var typeBadge = result.Type switch
        {
            "article" => t("search.type.article"),
            "recipe" => t("search.type.recipe"),
            "ingredient" => t("search.type.ingredient"),
            "page" => t("search.type.page"),
            _ => result.Type,
        };

        var link = result.Type switch
        {
            "article" => ctx.Url(SlugEntityType.Article, result.Id),
            "recipe" => ctx.Url(SlugEntityType.Recipe, result.Id),
            "page" => ctx.Url(SlugEntityType.Page, result.Id),
            _ => null,
        };

        var titleHtml = link is not null
            ? Html.Link(link, result.Title)
            : Html.Text(result.Title);

        var snippet = string.IsNullOrEmpty(result.Snippet)
            ? HtmlString.Empty
            : Html.Raw($"<p class=\"search-snippet\">{result.Snippet}</p>");

        return Html.Raw($"<span class=\"search-badge\">{Html.Encode(typeBadge)}</span> ") +
            titleHtml +
            snippet;
    }
}
