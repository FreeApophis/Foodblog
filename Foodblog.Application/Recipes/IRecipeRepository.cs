namespace Foodblog.Application.Recipes;

public interface IRecipeRepository
{
    ValueTask<IReadOnlyList<Recipe>> All();

    // All recipes carrying the tag identified by its case-folded slug.
    ValueTask<IReadOnlyList<Recipe>> FindByTag(string slug);

    ValueTask<Option<Recipe>> Find(RecipeId id);

    ValueTask<RecipeId> NextId();

    ValueTask Save(Recipe recipe);

    // Updates only the derived per-serving calorie figure, leaving the rest of the recipe untouched.
    // Used to recompute stored values without rewriting (and re-inserting) the recipe's child rows.
    ValueTask UpdateCalorificValue(RecipeId id, int value);

    ValueTask Delete(RecipeId id);
}
