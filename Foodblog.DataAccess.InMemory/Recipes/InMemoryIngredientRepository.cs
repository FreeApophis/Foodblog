using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Recipes;

public sealed class InMemoryIngredientRepository : IIngredientRepository
{
    private readonly ConcurrentDictionary<int, Ingredient> _ingredients = new();
    private int _nextId = 1;

    public ValueTask<IReadOnlyList<Ingredient>> All() =>
        ValueTask.FromResult<IReadOnlyList<Ingredient>>(
            _ingredients.Values.OrderBy(x => x.Name.Value).ToList());

    public ValueTask<Option<Ingredient>> Find(IngredientId id) =>
        ValueTask.FromResult(_ingredients.GetValueOrNone(id.Value));

    public ValueTask<IngredientId> NextId() =>
        ValueTask.FromResult(new IngredientId(Interlocked.Increment(ref _nextId)));

    public ValueTask Save(Ingredient ingredient)
    {
        _ingredients[ingredient.Id.Value] = ingredient;
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(IngredientId id)
    {
        _ingredients.TryRemove(id.Value, out _);
        return ValueTask.CompletedTask;
    }
}
