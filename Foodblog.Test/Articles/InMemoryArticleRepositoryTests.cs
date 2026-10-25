namespace Foodblog.Test.Articles;

public sealed class InMemoryArticleRepositoryTests : ArticleRepositoryContract
{
    protected override IArticleRepository CreateRepository() => new InMemoryArticleRepository();
}
