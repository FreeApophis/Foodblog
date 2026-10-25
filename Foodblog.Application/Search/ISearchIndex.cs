namespace Foodblog.Application.Search;

public interface ISearchIndex
{
    void IndexArticle(Article article);

    void IndexRecipe(Recipe recipe);

    void IndexIngredient(Ingredient ingredient);

    void IndexPage(Page page);

    void DeleteDocument(string type, int id);

    IReadOnlyList<SearchResult> Search(string query, int topN = 20);

    IReadOnlyList<string> Suggestions(string query);

    SearchIndexStatus Status();

    ValueTask RebuildAsync(
        IArticleRepository articles,
        IRecipeRepository recipes,
        IIngredientRepository ingredients,
        IPageRepository pages);
}
