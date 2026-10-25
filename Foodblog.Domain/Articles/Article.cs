namespace Foodblog.Domain.Articles;

public sealed record Article(
    ArticleId Id,
    ArticleTitle Title,
    ArticleTeaser Teaser,
    ArticleText Text,
    UserId AuthorId,
    DateTimeOffset PublishedAt,
    Option<ImageId> CoverImageId)
{
    public static Article Create(
        ArticleId id,
        ArticleTitle title,
        ArticleTeaser teaser,
        ArticleText text,
        UserId authorId,
        DateTimeOffset publishedAt,
        Option<ImageId> coverImageId = default) =>
        new(id, title, teaser, text, authorId, publishedAt, coverImageId);
}
