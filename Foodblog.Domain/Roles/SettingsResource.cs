namespace Foodblog.Domain.Roles;

public sealed record SettingsResource : IResource
{
    public string ResourceKey => "settings";
}
