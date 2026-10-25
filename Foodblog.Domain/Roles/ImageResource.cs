namespace Foodblog.Domain.Roles;

public sealed record ImageResource : IResource
{
    public string ResourceKey => "image";
}
