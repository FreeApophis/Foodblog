namespace Foodblog.Test.Recipes;

public sealed class InMemoryUnitRepositoryTests : UnitRepositoryContract
{
    protected override IUnitRepository CreateRepository() => new InMemoryUnitRepository();
}
