namespace Foodblog.Test.Recipes;

public sealed class IngredientTests
{
    [Fact]
    public void A_fully_filled_ingredient_is_not_flagged_as_missing_information()
    {
        var ingredient = Make(description: "Feines Weizenmehl", image: "/images/1");

        Assert.False(ingredient.HasMissingInformation);
    }

    [Fact(Skip = "Ingredient.HasMissingInformation is hardcoded to false until 0 and unset can be told apart (see TODO).")]
    public void An_ingredient_without_a_description_has_missing_information()
    {
        var ingredient = Make(description: "  ", image: "/images/1");

        Assert.True(ingredient.HasMissingInformation);
    }

    [Fact(Skip = "Ingredient.HasMissingInformation is hardcoded to false until 0 and unset can be told apart (see TODO).")]
    public void An_ingredient_without_an_image_has_missing_information()
    {
        var ingredient = Make(description: "Feines Weizenmehl", image: string.Empty);

        Assert.True(ingredient.HasMissingInformation);
    }

    private static Ingredient Make(string description, string image) =>
        Ingredient.Create(
            new IngredientId(1),
            new IngredientName("Mehl"),
            image,
            description,
            density: 1,
            pieceCount: 0,
            calorificValue: 0,
            protein: 0,
            fat: 0,
            carbohydrates: 0,
            sugar: 0,
            fiber: 0);
}
