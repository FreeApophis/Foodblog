namespace Foodblog.Test.Ingredients;

public sealed class IngredientFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var request = Build("Ei", "Hühnerei", "1.0", "1.0", "155", "13", "11", "1", "0.4", "0");

        var form = ValidatedAssert.IsSuccess(IngredientForm.Decode(request));

        Assert.Equal(new IngredientName("Ei"), form.Name);
        Assert.Equal(1.0m, form.Density);
        Assert.Equal(155m, form.CalorificValue);
        Assert.Equal(11m, form.Fat);
    }

    [Fact]
    public void Name_shorter_than_2_characters_returns_failure()
    {
        var request = Build("X", "Beschreibung", "1.0", "0", "0", "0", "0", "0", "0", "0");

        var errors = ValidatedAssert.IsFailure(IngredientForm.Decode(request));

        Assert.Contains("ingredient.error.name_too_short", errors);
    }

    [Fact]
    public void Name_of_2_characters_is_valid()
    {
        var request = Build("Ei", "Beschreibung", "1.0", "0", "0", "0", "0", "0", "0", "0");

        ValidatedAssert.IsSuccess(IngredientForm.Decode(request));
    }

    [Fact]
    public void Density_of_zero_or_less_returns_failure()
    {
        var request = Build("Salz", "Beschreibung", "0", "0", "0", "0", "0", "0", "0", "0");

        var errors = ValidatedAssert.IsFailure(IngredientForm.Decode(request));

        Assert.Contains("ingredient.error.density_invalid", errors);
    }

    [Fact]
    public void Density_not_a_number_returns_failure()
    {
        var request = Build("Salz", "Beschreibung", "abc", "0", "0", "0", "0", "0", "0", "0");

        var errors = ValidatedAssert.IsFailure(IngredientForm.Decode(request));

        Assert.Contains("ingredient.error.density_invalid", errors);
    }

    [Fact]
    public void Negative_piece_count_returns_failure()
    {
        var request = Build("Ei", "Beschreibung", "1.0", "-1", "0", "0", "0", "0", "0", "0");

        var errors = ValidatedAssert.IsFailure(IngredientForm.Decode(request));

        Assert.Contains("ingredient.error.piece_count_invalid", errors);
    }

    [Fact]
    public void Negative_calorific_value_returns_failure()
    {
        var request = Build("Ei", "Beschreibung", "1.0", "0", "-1", "0", "0", "0", "0", "0");

        var errors = ValidatedAssert.IsFailure(IngredientForm.Decode(request));

        Assert.Contains("ingredient.error.calorific_value_invalid", errors);
    }

    [Fact]
    public void Optional_image_and_description_may_be_empty()
    {
        var request = BuildFull(
            name: "Öl",
            description: string.Empty,
            image: string.Empty,
            density: "0.92",
            pieceCount: "0",
            calorificValue: "900",
            protein: "0",
            fat: "100",
            carbohydrates: "0",
            sugar: "0",
            fiber: "0");

        ValidatedAssert.IsSuccess(IngredientForm.Decode(request));
    }

    [Fact]
    public void All_nutrient_fields_are_preserved_with_correct_types()
    {
        var request = Build("Butter", "Süßrahmbutter", "0.96", "0", "740", "0.7", "83", "0.4", "0.4", "0");

        var form = ValidatedAssert.IsSuccess(IngredientForm.Decode(request));

        Assert.Equal(new IngredientName("Butter"), form.Name);
        Assert.Equal(0.96m, form.Density);
        Assert.Equal(740m, form.CalorificValue);
        Assert.Equal(83m, form.Fat);
    }

    [Fact]
    public void Multiple_errors_accumulate()
    {
        var request = Build("X", "Beschreibung", "0", "-1", "-1", "0", "0", "0", "0", "0");

        var errors = ValidatedAssert.IsFailure(IngredientForm.Decode(request));

        Assert.Contains("ingredient.error.name_too_short", errors);
        Assert.Contains("ingredient.error.density_invalid", errors);
        Assert.Contains("ingredient.error.piece_count_invalid", errors);
        Assert.Contains("ingredient.error.calorific_value_invalid", errors);
    }

    private static Request Build(
        string name,
        string description,
        string density,
        string pieceCount,
        string calorificValue,
        string protein,
        string fat,
        string carbohydrates,
        string sugar,
        string fiber) =>
        BuildFull(name, description, string.Empty, density, pieceCount, calorificValue, protein, fat, carbohydrates, sugar, fiber);

    private static Request BuildFull(
        string name,
        string description,
        string image,
        string density,
        string pieceCount,
        string calorificValue,
        string protein,
        string fat,
        string carbohydrates,
        string sugar,
        string fiber)
    {
        var form = new Dictionary<string, string>
        {
            ["name"] = name,
            ["description"] = description,
            ["image"] = image,
            ["density"] = density,
            ["piece_count"] = pieceCount,
            ["calorific_value"] = calorificValue,
            ["protein"] = protein,
            ["fat"] = fat,
            ["carbohydrates"] = carbohydrates,
            ["sugar"] = sugar,
            ["fiber"] = fiber,
        };
        return new Request(HttpMethod.Post, "/admin/ingredients", Empty, Empty, form, Empty);
    }

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
