namespace Foodblog.Application.Recipes;

public interface IIngredientRepository
{
    ValueTask<IReadOnlyList<Ingredient>> All();

    ValueTask<Option<Ingredient>> Find(IngredientId id);

    ValueTask<IngredientId> NextId();

    ValueTask Save(Ingredient ingredient);

    ValueTask Delete(IngredientId id);
}
