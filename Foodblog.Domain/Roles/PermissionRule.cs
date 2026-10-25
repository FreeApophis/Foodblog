namespace Foodblog.Domain.Roles;

public sealed record PermissionRule(string ActionName, string ResourceKey);
