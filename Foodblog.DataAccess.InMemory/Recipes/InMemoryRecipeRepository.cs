using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Recipes;

public sealed class InMemoryRecipeRepository : IRecipeRepository
{
    private readonly ConcurrentDictionary<int, Recipe> _recipes = new();
    private int _nextId = 1;

    public ValueTask<IReadOnlyList<Recipe>> All() =>
        ValueTask.FromResult<IReadOnlyList<Recipe>>(
            _recipes.Values.OrderBy(x => x.Name.Value).ToList());

    public ValueTask<IReadOnlyList<Recipe>> FindByTag(string slug) =>
        ValueTask.FromResult<IReadOnlyList<Recipe>>(
            _recipes.Values
                .Where(r => r.Tags.Any(tag => Slug.From(tag.Value) == slug))
                .OrderBy(r => r.Name.Value)
                .ToList());

    public ValueTask<Option<Recipe>> Find(RecipeId id) =>
        ValueTask.FromResult(_recipes.GetValueOrNone(id.Value));

    public ValueTask<RecipeId> NextId() =>
        ValueTask.FromResult(new RecipeId(Interlocked.Increment(ref _nextId)));

    public ValueTask Save(Recipe recipe)
    {
        _recipes[recipe.Id.Value] = recipe;
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateCalorificValue(RecipeId id, int value)
    {
        if (_recipes.TryGetValue(id.Value, out var recipe))
        {
            _recipes[id.Value] = recipe with { CalorificValue = value };
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(RecipeId id)
    {
        _recipes.TryRemove(id.Value, out _);
        return ValueTask.CompletedTask;
    }
}
