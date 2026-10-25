namespace Foodblog.Application.Pages;

public interface IPageRepository
{
    ValueTask<IReadOnlyList<Page>> All();

    ValueTask<Option<Page>> Find(PageId id);

    ValueTask<PageId> NextId();

    ValueTask Save(Page page);

    ValueTask Delete(PageId id);
}
