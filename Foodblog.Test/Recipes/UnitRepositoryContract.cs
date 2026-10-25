namespace Foodblog.Test.Recipes;

public abstract class UnitRepositoryContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_unit()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var unit = AUnit(id);

        await repo.Save(unit);

        Assert.Equal(Option.Some(unit), await repo.Find(id));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_id()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.Find(new UnitId(987_654)));
    }

    [Fact]
    public async Task NextId_returns_an_id_that_does_not_yet_exist()
    {
        var repo = CreateRepository();

        var id = await repo.NextId();

        FunctionalAssert.None(await repo.Find(id));
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
    public async Task All_returns_saved_units()
    {
        var repo = CreateRepository();
        var first = AUnit(await repo.NextId(), category: UnitCategory.Weight);
        var second = AUnit(await repo.NextId(), category: UnitCategory.Volume);

        await repo.Save(first);
        await repo.Save(second);

        var all = await repo.All();
        Assert.Contains(first, all);
        Assert.Contains(second, all);
    }

    [Fact]
    public async Task Save_replaces_an_existing_unit_with_the_same_id()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var original = AUnit(id, factor: 1m);
        var updated = AUnit(id, factor: 2m);

        await repo.Save(original);
        await repo.Save(updated);

        Assert.Equal(Option.Some(updated), await repo.Find(id));
    }

    [Fact]
    public async Task Delete_removes_the_unit()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        await repo.Save(AUnit(id));

        await repo.Delete(id);

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task Delete_is_idempotent_for_unknown_id()
    {
        var repo = CreateRepository();

        await repo.Delete(new UnitId(987_654));
    }

    protected abstract IUnitRepository CreateRepository();

    private static Unit AUnit(UnitId id, UnitCategory category = UnitCategory.Weight, decimal factor = 1m) =>
        new(id, $"unit.{id.Value}.name", $"unit.{id.Value}.abbr", category, factor);
}
