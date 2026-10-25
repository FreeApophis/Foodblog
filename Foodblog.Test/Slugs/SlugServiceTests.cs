namespace Foodblog.Test.Slugs;

public sealed class SlugServiceTests
{
    [Fact]
    public async Task Ensure_uses_the_normalized_base_slug_when_free()
    {
        var repo = new InMemorySlugRepository();
        var service = new SlugService(repo);

        var slug = await service.Ensure(SlugEntityType.Recipe, 1, "Crème brûlée");

        Assert.Equal("creme-brulee", slug);
        Assert.Equal(Option.Some(new SlugTarget(SlugEntityType.Recipe, 1)), await repo.FindTarget("creme-brulee"));
    }

    [Fact]
    public async Task Ensure_suffixes_when_another_entity_owns_the_base_slug()
    {
        var repo = new InMemorySlugRepository();
        var service = new SlugService(repo);
        await service.Ensure(SlugEntityType.Recipe, 1, "Pizza");

        var second = await service.Ensure(SlugEntityType.Article, 2, "Pizza");

        Assert.Equal("pizza-2", second);
    }

    [Fact]
    public async Task Ensure_is_idempotent_for_the_same_entity()
    {
        var repo = new InMemorySlugRepository();
        var service = new SlugService(repo);

        var first = await service.Ensure(SlugEntityType.Page, 4, "Über uns");
        var again = await service.Ensure(SlugEntityType.Page, 4, "Über uns");

        Assert.Equal("ueber-uns", first);
        Assert.Equal("ueber-uns", again);
        Assert.Single(await repo.SlugsFor(SlugEntityType.Page));
    }

    [Fact]
    public async Task Ensure_keeps_the_base_slug_for_its_owner_when_re_run_after_others_exist()
    {
        var repo = new InMemorySlugRepository();
        var service = new SlugService(repo);
        await service.Ensure(SlugEntityType.Recipe, 1, "Pasta");
        await service.Ensure(SlugEntityType.Recipe, 2, "Pasta");

        var rerun = await service.Ensure(SlugEntityType.Recipe, 1, "Pasta");

        Assert.Equal("pasta", rerun);
    }
}
