namespace Foodblog.Application.Slugs;

public interface ISlugRepository
{
    // Resolves a slug to the entity that currently owns it. None when the slug is unknown.
    ValueTask<Option<SlugTarget>> FindTarget(string slug);

    // The current slug for a single entity, for building one URL. None when not yet slugged.
    ValueTask<Option<string>> FindSlug(string entityType, int entityId);

    // All current slugs for a type, keyed by entity id — for list pages that link many entities.
    ValueTask<IReadOnlyDictionary<int, string>> SlugsFor(string entityType);

    // Sets the slug for an entity, replacing any previous slug it had (no history).
    ValueTask Upsert(string entityType, int entityId, string slug);
}
