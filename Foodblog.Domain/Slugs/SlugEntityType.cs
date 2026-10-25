namespace Foodblog.Domain.Slugs;

// The slug-registry discriminators. Stored verbatim in the slugs table's entity_type column.
public static class SlugEntityType
{
    public const string Article = "article";
    public const string Recipe = "recipe";
    public const string Page = "page";
    public const string Ingredient = "ingredient";
    public const string Tag = "tag";
}
