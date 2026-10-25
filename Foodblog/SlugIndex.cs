using System.Globalization;

namespace Foodblog;

// Per-request lookup of entity slugs for building canonical view URLs in the rendering layer.
// Built once per request from ISlugRepository (see SlugMiddleware) and threaded into views via
// ViewContext. Falls back to the numeric id when an entity has no registered slug — or when no
// slug repository is configured at all (minimal test envs use SlugIndex.Empty) — so URL
// generation always produces a working path.
public sealed class SlugIndex
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> _byType;

    // Tags are referenced by name in the domain (RecipeTag), not by id, so their public slugs are
    // looked up by case-folded name (Slug.From) rather than entity id.
    private readonly IReadOnlyDictionary<string, string> _tagSlugs;

    public SlugIndex(
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> byType,
        IReadOnlyDictionary<string, string>? tagSlugs = null)
    {
        _byType = byType;
        _tagSlugs = tagSlugs ?? new Dictionary<string, string>();
    }

    public static SlugIndex Empty { get; } = new(new Dictionary<string, IReadOnlyDictionary<int, string>>());

    // The slug for an entity, or its numeric id as a string when none is registered.
    public string For(string entityType, int id) =>
        _byType.GetValueOrNone(entityType)
            .SelectMany(map => map.GetValueOrNone(id))
            .GetOrElse(id.ToString(CultureInfo.InvariantCulture));

    // The canonical path for an entity, e.g. "/recipes/ruehrkuchen".
    public string Url(string entityType, int id) => $"/{Prefix(entityType)}/{For(entityType, id)}";

    // The canonical /tag/{slug} path for a tag name. Falls back to the freshly-computed slug when the
    // tag is unregistered (no registry, or a brand-new tag) — which equals the registered slug
    // whenever there was no cross-type collision.
    public string TagUrl(string name)
    {
        var folded = Slug.From(name);
        return $"/tag/{_tagSlugs.GetValueOrNone(folded).GetOrElse(folded)}";
    }

    private static string Prefix(string entityType) => entityType switch
    {
        SlugEntityType.Article => "articles",
        SlugEntityType.Recipe => "recipes",
        SlugEntityType.Page => "pages",
        SlugEntityType.Ingredient => "ingredients",
        SlugEntityType.Tag => "tag",
        _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown slug entity type"),
    };
}
