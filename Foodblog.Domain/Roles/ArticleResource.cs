namespace Foodblog.Domain.Roles;

public sealed record ArticleResource : IResource
{
    public string ResourceKey => "article";
}
