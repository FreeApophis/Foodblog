namespace Foodblog.Domain.Roles;

public sealed record PageResource : IResource
{
    public string ResourceKey => "page";
}
