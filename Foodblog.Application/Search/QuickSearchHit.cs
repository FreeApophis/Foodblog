namespace Foodblog.Application.Search;

// A single quicksearch suggestion. Category is one of "tag", "article", "recipe", "ingredient"
// and determines how the view turns it into a link: tags use Slug, while articles, recipes and
// ingredients use Id.
public sealed record QuickSearchHit(string Category, string Label, int? Id = null, string? Slug = null);
