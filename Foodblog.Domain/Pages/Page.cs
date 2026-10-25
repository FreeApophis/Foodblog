namespace Foodblog.Domain.Pages;

public sealed record Page(
    PageId Id,
    PageTitle Title,
    PageContent Content)
{
    public static Page Create(PageId id, PageTitle title, PageContent content)
        => new(id, title, content);
}
