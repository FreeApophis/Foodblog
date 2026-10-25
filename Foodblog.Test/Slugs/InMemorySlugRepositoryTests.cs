namespace Foodblog.Test.Slugs;

public sealed class InMemorySlugRepositoryTests : SlugRepositoryContract
{
    protected override ISlugRepository Create() => new InMemorySlugRepository();
}
