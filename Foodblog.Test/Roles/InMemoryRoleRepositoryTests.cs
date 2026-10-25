namespace Foodblog.Test.Roles;

public sealed class InMemoryRoleRepositoryTests : RoleRepositoryContract
{
    protected override IRoleRepository CreateRepository() => new InMemoryRoleRepository();
}
