namespace Foodblog.Domain.Roles;

public sealed record SearchResource : IResource
{
    public string ResourceKey => "search";
}
