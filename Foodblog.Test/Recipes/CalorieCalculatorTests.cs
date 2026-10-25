namespace Foodblog.Test.Recipes;

public sealed class CalorieCalculatorTests
{
    // The catalog stores energy as kilojoules per 100 g; the calculator returns kilocalories.
    private const decimal Kj = CalorieCalculator.KilojoulesPerKilocalorie;

    [Fact]
    public void Calorific_value_is_kilojoules_per_100g_and_returned_as_kilocalories()
    {
        var ingredient = AnIngredient(calorificValue: 1000m); // 1000 kJ / 100 g

        // 100 g -> 1000 kJ -> 1000/4.184 kcal
        Assert.Equal(1000m / Kj, CalorieCalculator.ForIngredient(100m, Gram, ingredient));
    }

    [Fact]
    public void Weight_units_convert_directly_to_grams()
    {
        var flour = AnIngredient(calorificValue: 350m); // kJ / 100 g

        Assert.Equal(350m / Kj, CalorieCalculator.ForIngredient(100m, Gram, flour));
        Assert.Equal(3500m / Kj, CalorieCalculator.ForIngredient(1m, Kilogram, flour));
    }

    [Fact]
    public void Volume_units_convert_to_grams_via_density()
    {
        var milk = AnIngredient(calorificValue: 64m, density: 1.03m);

        // 200 ml * 1.03 g/ml = 206 g -> 206/100 * 64 kJ -> /4.184 kcal
        Assert.Equal(206m / 100m * 64m / Kj, CalorieCalculator.ForIngredient(200m, Milliliter, milk));
    }

    [Fact]
    public void Piece_units_convert_to_grams_via_piece_weight()
    {
        var egg = AnIngredient(calorificValue: 155m, pieceCount: 60m);

        // 2 pieces * 60 g = 120 g -> 120/100 * 155 kJ -> /4.184 kcal
        Assert.Equal(120m / 100m * 155m / Kj, CalorieCalculator.ForIngredient(2m, Piece, egg));
    }

    [Fact]
    public void PerServing_sums_ingredients_and_divides_by_portions()
    {
        var flour = AnIngredient(calorificValue: 1464m); // ~350 kcal / 100 g expressed as kJ
        var catalog = new Dictionary<IngredientId, Ingredient> { [flour.Id] = flour };
        IReadOnlyList<RecipeIngredient> ingredients = [new RecipeIngredient(flour.Id, 200m, Gram)];

        // 200 g -> 2928 kJ -> 699.8 kcal total, 2 servings -> 350 per serving
        Assert.Equal(350, CalorieCalculator.PerServing(ingredients, portions: 2, catalog));
    }

    [Fact]
    public void PerServing_is_zero_when_there_are_no_portions()
    {
        var flour = AnIngredient(calorificValue: 350m);
        var catalog = new Dictionary<IngredientId, Ingredient> { [flour.Id] = flour };
        IReadOnlyList<RecipeIngredient> ingredients = [new RecipeIngredient(flour.Id, 200m, Gram)];

        Assert.Equal(0, CalorieCalculator.PerServing(ingredients, portions: 0, catalog));
    }

    [Fact]
    public void PerServing_ignores_ingredients_missing_from_the_catalog()
    {
        IReadOnlyList<RecipeIngredient> ingredients = [new RecipeIngredient(new IngredientId(99), 200m, Gram)];

        Assert.Equal(0, CalorieCalculator.PerServing(ingredients, portions: 2, new Dictionary<IngredientId, Ingredient>()));
    }

    private static Ingredient AnIngredient(decimal calorificValue, decimal density = 1m, decimal pieceCount = 0m) =>
        Ingredient.Create(
            new IngredientId(1),
            new IngredientName("Zutat"),
            image: string.Empty,
            description: string.Empty,
            density: density,
            pieceCount: pieceCount,
            calorificValue: calorificValue,
            protein: 0,
            fat: 0,
            carbohydrates: 0,
            sugar: 0,
            fiber: 0);
}
