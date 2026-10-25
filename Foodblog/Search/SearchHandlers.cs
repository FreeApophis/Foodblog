namespace Foodblog.Search;

public static class SearchHandlers
{
    public static App Search => request => env =>
    {
        var q = request.Query.GetValueOrNone("q").GetOrElse(string.Empty).Trim();

        if (string.IsNullOrEmpty(q) || env.Search is null)
        {
            return ValueTask.FromResult(Response.Html(SearchViews.Empty(env.Ctx)));
        }

        var results = env.Search.Search(q);
        var suggestions = results.Count < 3 ? env.Search.Suggestions(q) : [];

        return ValueTask.FromResult(Response.Html(
            SearchViews.Results(q, results, suggestions, env.Ctx)));
    };

    // Live typeahead fragment for the nav search box: an empty body clears the dropdown, otherwise a
    // category-grouped list of LIKE matches.
    public static App Quick => request => async env =>
    {
        var q = request.Query.GetValueOrNone("q").GetOrElse(string.Empty).Trim();

        if (q.Length == 0 || env.QuickSearch is null)
        {
            return Response.Html(string.Empty);
        }

        var hits = await env.QuickSearch.Search(q);

        return Response.Html(SearchViews.QuickResults(hits, env.Ctx));
    };
}
