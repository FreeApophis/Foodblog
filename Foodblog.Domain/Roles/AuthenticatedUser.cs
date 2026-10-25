namespace Foodblog.Domain.Roles;

public sealed class AuthenticatedUser : IPrincipal
{
    private readonly User _user;
    private readonly IReadOnlyList<Role> _roles;

    public AuthenticatedUser(User user, IReadOnlyList<Role> roles)
    {
        _user = user;
        _roles = roles;
    }

    public bool IsAuthenticated => true;

    public UserId Id => _user.Id;

    public Email Email => _user.Email;

    public DisplayName DisplayName => _user.DisplayName;

    public IReadOnlyList<string> RoleNames => _user.RoleNames;

    public Option<ImageId> AvatarImageId => _user.AvatarImageId;

    public bool Can<TAction>(IResource resource)
        where TAction : IAction
    {
        var actionName = typeof(TAction).Name;
        var resourceKey = resource.ResourceKey;
        return _roles.Any(role => role.Rules.Any(rule =>
            rule.ActionName == actionName && rule.ResourceKey == resourceKey));
    }
}
