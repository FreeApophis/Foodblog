using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Tags;

// A simple seedable tag dictionary for tests. Production uses the SQLite repository, which reads the
// normalized `tags` table and resolves slugs through the central registry. Seeded tags are assigned
// sequential synthetic ids so the (id, name) projections behave like the real store.
public sealed class InMemoryTagRepository : ITagRepository
{
    private readonly ConcurrentDictionary<string, Tag> _bySlug = new();
    private readonly ConcurrentDictionary<int, string> _names = new();
    private int _nextId;

    public InMemoryTagRepository(params Tag[] seed)
    {
        foreach (var tag in seed)
        {
            var id = Interlocked.Increment(ref _nextId);
            _bySlug[tag.Slug] = tag;
            _names[id] = tag.Name;
        }
    }

    public ValueTask<Option<Tag>> FindBySlug(string slug) =>
        ValueTask.FromResult(_bySlug.GetValueOrNone(slug));

    public ValueTask<IReadOnlyList<TagEntry>> All() =>
        ValueTask.FromResult<IReadOnlyList<TagEntry>>(
            _names.Select(kv => new TagEntry(kv.Key, kv.Value)).ToList());

    public ValueTask<Option<int>> FindIdByName(string name) =>
        ValueTask.FromResult(_names
            .Where(kv => string.Equals(kv.Value, name, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .OrderBy(Functional.Identity)
            .FirstOrNone());
}
