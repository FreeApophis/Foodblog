namespace Foodblog;

// Loads the slug registry once per request and threads it into Env, so the rendering layer can
// build canonical /type/{slug} URLs synchronously (ViewContext.Url / SlugIndex). When no slug
// repository is configured it does nothing and URLs fall back to numeric ids.
public static class SlugMiddleware
{
    private static readonly string[] Types =
    [
        SlugEntityType.Article,
        SlugEntityType.Recipe,
        SlugEntityType.Page,
        SlugEntityType.Ingredient,
    ];

    public static Middleware Create() => next => request => async env =>
    {
        if (env.Slugs is not { } slugs)
        {
            return await next(request)(env);
        }

        var byType = new Dictionary<string, IReadOnlyDictionary<int, string>>();
        foreach (var type in Types)
        {
            byType[type] = await slugs.SlugsFor(type);
        }

        // Tags are referenced by name (RecipeTag), so build a folded-name → public-slug map by
        // joining the tag list to its registered slugs.
        var tagSlugs = new Dictionary<string, string>();
        var tagSlugById = await slugs.SlugsFor(SlugEntityType.Tag);
        if (env.Tags is { } tagRepo)
        {
            foreach (var tag in await tagRepo.All())
            {
                if (tagSlugById.GetValueOrNone(tag.Id) is [var slug])
                {
                    tagSlugs[Slug.From(tag.Name)] = slug;
                }
            }
        }

        return await next(request)(env with { SlugIndex = new SlugIndex(byType, tagSlugs) });
    };
}
