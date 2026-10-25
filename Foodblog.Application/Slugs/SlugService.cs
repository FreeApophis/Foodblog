namespace Foodblog.Application.Slugs;

// Generates and registers globally-unique slugs. The base slug comes from the entity's
// title/name via Slug.From; collisions with *other* entities are resolved by suffixing
// "-2", "-3", … The entity's own existing slug never counts as a collision, so re-running
// is idempotent.
public sealed class SlugService
{
    private readonly ISlugRepository _slugs;

    public SlugService(ISlugRepository slugs) => _slugs = slugs;

    public async ValueTask<string> Ensure(string entityType, int entityId, string sourceText)
    {
        var baseSlug = Slug.From(sourceText);
        var candidate = baseSlug;
        var suffix = 2;

        while (await IsTakenByOther(candidate, entityType, entityId))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        await _slugs.Upsert(entityType, entityId, candidate);
        return candidate;
    }

    private async ValueTask<bool> IsTakenByOther(string slug, string entityType, int entityId) =>
        await _slugs.FindTarget(slug) is [var target] &&
        !(target.EntityType == entityType && target.EntityId == entityId);
}
