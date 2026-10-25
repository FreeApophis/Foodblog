namespace Foodblog.Test.Pages;

public sealed class InMemoryPageRepositoryTests : PageRepositoryContract
{
    protected override IPageRepository CreateRepository() => new InMemoryPageRepository();
}
