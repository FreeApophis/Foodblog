namespace Foodblog.Application.Search;

// Lightweight category typeahead backing the nav search box. Unlike the full-text ISearchIndex,
// this is a direct, prefix-style LIKE lookup over the live tables, returning a short list grouped
// by category in a fixed order: tags, then articles, then recipes, then ingredients.
public interface IQuickSearch
{
    ValueTask<IReadOnlyList<QuickSearchHit>> Search(string term);
}
