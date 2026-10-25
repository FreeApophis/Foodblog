namespace Foodblog.Test.Roles;

public abstract class RoleRepositoryContract
{
    [Fact]
    public async Task Save_then_FindById_returns_the_saved_role()
    {
        var repo = CreateRepository();
        var role = ARole(await repo.NextId());

        await repo.Save(role);

        Assert.Equal(Option.Some(role), await repo.FindById(role.Id));
    }

    [Fact]
    public async Task FindById_returns_none_for_an_unknown_id()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.FindById(new RoleId(987_654)));
    }

    [Fact]
    public async Task Save_then_FindByName_returns_the_saved_role()
    {
        var repo = CreateRepository();
        var role = ARole(await repo.NextId(), "Admin");

        await repo.Save(role);

        Assert.Equal(Option.Some(role), await repo.FindByName("Admin"));
    }

    [Fact]
    public async Task FindByName_returns_none_for_an_unknown_name()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.FindByName("Unbekannt"));
    }

    [Fact]
    public async Task NextId_returns_distinct_values_across_calls()
    {
        var repo = CreateRepository();

        var first = await repo.NextId();
        var second = await repo.NextId();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task All_returns_all_saved_roles()
    {
        var repo = CreateRepository();
        var roleA = ARole(await repo.NextId(), "Admin");
        var roleB = ARole(await repo.NextId(), "Benutzer");

        await repo.Save(roleA);
        await repo.Save(roleB);

        var all = await repo.All();
        Assert.Contains(roleA, all);
        Assert.Contains(roleB, all);
    }

    protected abstract IRoleRepository CreateRepository();

    private static Role ARole(RoleId id, string name = "Rolle") =>
        Role.Create(id, name);
}
