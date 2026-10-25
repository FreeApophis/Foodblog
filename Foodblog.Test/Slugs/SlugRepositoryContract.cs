namespace Foodblog.Test.Slugs;

public abstract class SlugRepositoryContract
{
    [Fact]
    public async Task Upsert_then_FindTarget_resolves_to_the_entity()
    {
        var repo = Create();

        await repo.Upsert(SlugEntityType.Recipe, 7, "ruehrkuchen");

        Assert.Equal(Option.Some(new SlugTarget(SlugEntityType.Recipe, 7)), await repo.FindTarget("ruehrkuchen"));
    }

    [Fact]
    public async Task FindTarget_returns_none_for_an_unknown_slug()
    {
        var repo = Create();

        FunctionalAssert.None(await repo.FindTarget("does-not-exist"));
    }

    [Fact]
    public async Task FindSlug_returns_the_current_slug_for_an_entity()
    {
        var repo = Create();
        await repo.Upsert(SlugEntityType.Page, 3, "impressum");

        Assert.Equal(Option.Some("impressum"), await repo.FindSlug(SlugEntityType.Page, 3));
    }

    [Fact]
    public async Task Upsert_replaces_the_previous_slug_for_the_same_entity()
    {
        var repo = Create();
        await repo.Upsert(SlugEntityType.Article, 5, "alt");

        await repo.Upsert(SlugEntityType.Article, 5, "neu");

        FunctionalAssert.None(await repo.FindTarget("alt"));
        Assert.Equal(Option.Some(new SlugTarget(SlugEntityType.Article, 5)), await repo.FindTarget("neu"));
        Assert.Equal(Option.Some("neu"), await repo.FindSlug(SlugEntityType.Article, 5));
    }

    [Fact]
    public async Task SlugsFor_returns_only_the_requested_type_keyed_by_id()
    {
        var repo = Create();
        await repo.Upsert(SlugEntityType.Recipe, 1, "pizza");
        await repo.Upsert(SlugEntityType.Recipe, 2, "pasta");
        await repo.Upsert(SlugEntityType.Article, 1, "willkommen");

        var slugs = await repo.SlugsFor(SlugEntityType.Recipe);

        Assert.Equal(2, slugs.Count);
        Assert.Equal("pizza", slugs[1]);
        Assert.Equal("pasta", slugs[2]);
    }

    protected abstract ISlugRepository Create();
}
