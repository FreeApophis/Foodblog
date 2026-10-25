namespace Foodblog.Domain.Recipes;

// Derives calorie figures from the ingredient catalog. An amount is first converted to grams using
// the unit's category — weight units scale directly, volume units via the ingredient's density
// (g/ml), piece units via its piece weight (g) — then multiplied by the calorific value, which is
// stored as kilojoules per 100 g, and finally converted from kilojoules to kilocalories.
public static class CalorieCalculator
{
    // The calorific value is stored in kilojoules; energy figures are reported in kilocalories.
    public const decimal KilojoulesPerKilocalorie = 4.184m;

    public static decimal ForIngredient(decimal amount, Unit unit, Ingredient ingredient) =>
        ToGrams(amount, unit, ingredient) / 100m * ingredient.CalorificValue / KilojoulesPerKilocalorie;

    // The whole meal's calories divided by the serving count, rounded to a whole number. Ingredients
    // not present in the catalog contribute nothing. Returns 0 when there are no portions, which
    // callers treat as "unknown" (also avoids a divide-by-zero).
    public static int PerServing(
        IReadOnlyList<RecipeIngredient> ingredients,
        int portions,
        IReadOnlyDictionary<IngredientId, Ingredient> catalog)
    {
        if (portions <= 0)
        {
            return 0;
        }

        decimal total = 0m;
        foreach (var ri in ingredients)
        {
            if (catalog.TryGetValue(ri.IngredientId, out var ingredient))
            {
                total += ForIngredient(ri.Amount, ri.Unit, ingredient);
            }
        }

        return (int)Math.Round(total / portions, MidpointRounding.AwayFromZero);
    }

    private static decimal ToGrams(decimal amount, Unit unit, Ingredient ingredient) =>
        unit.Category switch
        {
            UnitCategory.Weight => amount * unit.Factor * 1000m,                     // factor -> kg, *1000 -> g
            UnitCategory.Volume => amount * unit.Factor * 1000m * ingredient.Density, // factor -> l, *1000 -> ml, *density -> g
            UnitCategory.Piece => amount * unit.Factor * ingredient.PieceCount,       // factor -> pieces, *weight -> g
            _ => 0m,
        };
}
