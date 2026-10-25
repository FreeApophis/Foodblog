namespace Foodblog.Domain.Roles;

public sealed record Role(RoleId Id, string Name, IReadOnlyList<PermissionRule> Rules)
{
    public static Role Create(RoleId id, string name) =>
        new(id, name, []);

    public Role AddRule(PermissionRule rule) =>
        this with { Rules = [..Rules, rule] };

    public Role RemoveRule(PermissionRule rule) =>
        this with { Rules = Rules.Where(r => r != rule).ToList() };

    public bool Equals(Role? other) =>
        other is not null &&
        Id == other.Id &&
        Name == other.Name &&
        Rules.SequenceEqual(other.Rules);

    public override int GetHashCode() => Id.GetHashCode();
}
