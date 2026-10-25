namespace Foodblog.Application.Roles;

public interface IRoleRepository
{
    ValueTask<IReadOnlyList<Role>> All();

    ValueTask<Option<Role>> FindById(RoleId id);

    ValueTask<Option<Role>> FindByName(string name);

    ValueTask<RoleId> NextId();

    ValueTask Save(Role role);

    ValueTask Delete(RoleId id);
}
