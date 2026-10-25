namespace Foodblog.Domain.Roles;

public sealed record RoleResource : IResource
{
    public string ResourceKey => "role";
}
