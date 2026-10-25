namespace Foodblog.DataAccess.Search;

// A test-friendly quicksearch over the in-memory repositories. Mirrors the SQLite implementation's
// behaviour — substring match, per-category caps, category ordering — without a database. Tags are
// supplied directly since the in-memory tag repository is a slug-keyed lookup, not an enumeration.
public sealed class InMemoryQuickSearch : IQuickSearch
{
    private readonly IArticleRepository _articles;
    private readonly IRecipeRepository _recipes;
    private readonly IIngredientRepository _ingredients;
    private readonly IReadOnlyList<Tag> _tags;

    public InMemoryQuickSearch(
        IArticleRepository articles,
        IRecipeRepository recipes,
        IIngredientRepository ingredients,
        params Tag[] tags)
    {
        _articles = articles;
        _recipes = recipes;
        _ingredients = ingredients;
        _tags = tags;
    }

    public async ValueTask<IReadOnlyList<QuickSearchHit>> Search(string term)
    {
        var query = term.Trim();
        if (query.Length == 0)
        {
            return [];
        }

        bool Matches(string value) => value.Contains(query, StringComparison.OrdinalIgnoreCase);

        var hits = new List<QuickSearchHit>();

        hits.AddRange(_tags
            .Where(tag => Matches(tag.Name))
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .Select(tag => new QuickSearchHit("tag", tag.Name, Slug: tag.Slug)));

        hits.AddRange((await _articles.All())
            .Where(article => Matches(article.Title.Value) || Matches(article.Text.Value))
            .OrderBy(article => article.Title.Value, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(article => new QuickSearchHit("article", article.Title.Value, Id: article.Id.Value)));

        hits.AddRange((await _recipes.All())
            .Where(recipe => Matches(recipe.Name.Value))
            .OrderBy(recipe => recipe.Name.Value, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(recipe => new QuickSearchHit("recipe", recipe.Name.Value, Id: recipe.Id.Value)));

        hits.AddRange((await _ingredients.All())
            .Where(ingredient => Matches(ingredient.Name.Value))
            .OrderBy(ingredient => ingredient.Name.Value, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(ingredient => new QuickSearchHit("ingredient", ingredient.Name.Value, Id: ingredient.Id.Value)));

        return hits;
    }
}
