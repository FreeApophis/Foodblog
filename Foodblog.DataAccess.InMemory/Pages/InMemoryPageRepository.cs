using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Pages;

public sealed class InMemoryPageRepository : IPageRepository
{
    private readonly ConcurrentDictionary<int, Page> _pages = new();
    private int _nextId;

    public ValueTask<IReadOnlyList<Page>> All() =>
        ValueTask.FromResult<IReadOnlyList<Page>>(
            _pages.Values.OrderBy(p => p.Id.Value).ToList());

    public ValueTask<Option<Page>> Find(PageId id) =>
        ValueTask.FromResult(_pages.GetValueOrNone(id.Value));

    public ValueTask<PageId> NextId() =>
        ValueTask.FromResult(new PageId(Interlocked.Increment(ref _nextId)));

    public ValueTask Save(Page page)
    {
        _pages[page.Id.Value] = page;
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(PageId id)
    {
        _pages.TryRemove(id.Value, out _);
        return ValueTask.CompletedTask;
    }
}
