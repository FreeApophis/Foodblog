namespace Foodblog.Test.Pages;

public abstract class PageRepositoryContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_page()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var page = APage(id);

        await repo.Save(page);

        Assert.Equal(Option.Some(page), await repo.Find(id));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_id()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.Find(new PageId(987_654)));
    }

    [Fact]
    public async Task NextId_returns_an_id_that_does_not_yet_exist()
    {
        var repo = CreateRepository();

        var id = await repo.NextId();

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task NextId_returns_distinct_values_across_calls()
    {
        var repo = CreateRepository();

        var first = await repo.NextId();
        var second = await repo.NextId();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task All_returns_saved_pages_in_ascending_id_order()
    {
        var repo = CreateRepository();
        var first = APage(await repo.NextId(), title: "Erste");
        var second = APage(await repo.NextId(), title: "Zweite");

        await repo.Save(second);
        await repo.Save(first);

        var saved = (await repo.All())
            .Where(p => p.Id == first.Id || p.Id == second.Id)
            .ToList();

        Assert.Equal(new[] { first, second }, saved);
    }

    [Fact]
    public async Task Save_replaces_an_existing_page_with_the_same_id()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var original = APage(id, title: "Original");
        var updated = APage(id, title: "Aktualisiert");

        await repo.Save(original);
        await repo.Save(updated);

        Assert.Equal(Option.Some(updated), await repo.Find(id));
    }

    [Fact]
    public async Task Delete_removes_the_page()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        await repo.Save(APage(id));

        await repo.Delete(id);

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task Delete_is_idempotent_for_unknown_id()
    {
        var repo = CreateRepository();

        await repo.Delete(new PageId(987_654));
    }

    protected abstract IPageRepository CreateRepository();

    private static Page APage(PageId id, string title = "Über uns") =>
        Page.Create(id, new PageTitle(title), new PageContent("Inhalt der Seite mit [b]BBCode[/b]."));
}
