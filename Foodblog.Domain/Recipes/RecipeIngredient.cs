namespace Foodblog.Domain.Recipes;

public sealed record RecipeIngredient(IngredientId IngredientId, decimal Amount, Unit Unit);
