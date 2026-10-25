using System.Collections.Concurrent;
using Foodblog.Application.Slugs;
using Foodblog.Domain.Slugs;

namespace Foodblog.DataAccess.Slugs;

public sealed class InMemorySlugRepository : ISlugRepository
{
    private readonly ConcurrentDictionary<string, SlugTarget> _bySlug = new();

    public ValueTask<Option<SlugTarget>> FindTarget(string slug) =>
        ValueTask.FromResult(_bySlug.GetValueOrNone(slug));

    public ValueTask<Option<string>> FindSlug(string entityType, int entityId) =>
        ValueTask.FromResult(_bySlug
            .Where(kv => kv.Value.EntityType == entityType && kv.Value.EntityId == entityId)
            .Select(kv => kv.Key)
            .FirstOrNone());

    public ValueTask<IReadOnlyDictionary<int, string>> SlugsFor(string entityType) =>
        ValueTask.FromResult<IReadOnlyDictionary<int, string>>(_bySlug
            .Where(kv => kv.Value.EntityType == entityType)
            .ToDictionary(kv => kv.Value.EntityId, kv => kv.Key));

    public ValueTask Upsert(string entityType, int entityId, string slug)
    {
        foreach (var stale in _bySlug
            .Where(kv => kv.Value.EntityType == entityType && kv.Value.EntityId == entityId)
            .Select(kv => kv.Key)
            .ToList())
        {
            _bySlug.TryRemove(stale, out _);
        }

        _bySlug[slug] = new SlugTarget(entityType, entityId);
        return ValueTask.CompletedTask;
    }
}
