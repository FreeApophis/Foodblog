namespace Foodblog.Application.Articles;

public interface IArticleRepository
{
    ValueTask<IReadOnlyList<Article>> All();

    // All articles carrying the tag identified by its case-folded slug, newest first.
    ValueTask<IReadOnlyList<Article>> FindByTag(string slug);

    ValueTask<Option<Article>> Find(ArticleId id);

    ValueTask<ArticleId> NextId();

    ValueTask Save(Article article);

    ValueTask Delete(ArticleId id);
}
