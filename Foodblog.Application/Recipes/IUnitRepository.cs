namespace Foodblog.Application.Recipes;

public interface IUnitRepository
{
    ValueTask<IReadOnlyList<Unit>> All();

    ValueTask<Option<Unit>> Find(UnitId id);

    ValueTask<UnitId> NextId();

    ValueTask Save(Unit unit);

    ValueTask Delete(UnitId id);
}
