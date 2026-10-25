namespace Foodblog.Test.Search;

// A no-op ISearchIndex for handler tests: records whether RebuildAsync was called and returns a
// canned status. Avoids spinning up a real on-disk LeanCorpus index in handler tests.
public sealed class FakeSearchIndex : ISearchIndex
{
    public bool Rebuilt { get; private set; }

    public SearchIndexStatus StatusValue { get; set; } =
        new(Option<DateTimeOffset>.None, 0, 0, 0, 0);

    public void IndexArticle(Article article)
    {
    }

    public void IndexRecipe(Recipe recipe)
    {
    }

    public void IndexIngredient(Ingredient ingredient)
    {
    }

    public void IndexPage(Page page)
    {
    }

    public void DeleteDocument(string type, int id)
    {
    }

    public IReadOnlyList<SearchResult> Search(string query, int topN = 20) => [];

    public IReadOnlyList<string> Suggestions(string query) => [];

    public SearchIndexStatus Status() => StatusValue;

    public ValueTask RebuildAsync(
        IArticleRepository articles,
        IRecipeRepository recipes,
        IIngredientRepository ingredients,
        IPageRepository pages)
    {
        Rebuilt = true;
        return ValueTask.CompletedTask;
    }
}
