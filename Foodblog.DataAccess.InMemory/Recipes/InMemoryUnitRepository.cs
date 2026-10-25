using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Recipes;

public sealed class InMemoryUnitRepository : IUnitRepository
{
    private readonly ConcurrentDictionary<int, Unit> _units = new();
    private int _nextId;

    // Pre-seed the well-known units so recipe/ingredient fixtures that reference Unit.Gram etc.
    // resolve against a populated catalog, mirroring the seeded production database.
    public InMemoryUnitRepository()
    {
        foreach (var unit in new[]
        {
            Unit.Gram, Unit.Kilogram, Unit.Milliliter, Unit.Liter, Unit.Deciliter,
            Unit.Tablespoon, Unit.Teaspoon, Unit.Pinch, Unit.Piece,
        })
        {
            _units[unit.Id.Value] = unit;
        }

        _nextId = _units.Keys.DefaultIfEmpty(0).Max();
    }

    public ValueTask<IReadOnlyList<Unit>> All() =>
        ValueTask.FromResult<IReadOnlyList<Unit>>(_units.Values.OrderBy(x => x.Id.Value).ToList());

    public ValueTask<Option<Unit>> Find(UnitId id) =>
        ValueTask.FromResult(_units.GetValueOrNone(id.Value));

    public ValueTask<UnitId> NextId() =>
        ValueTask.FromResult(new UnitId(Interlocked.Increment(ref _nextId)));

    public ValueTask Save(Unit unit)
    {
        _units[unit.Id.Value] = unit;
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(UnitId id)
    {
        _units.TryRemove(id.Value, out _);
        return ValueTask.CompletedTask;
    }
}
