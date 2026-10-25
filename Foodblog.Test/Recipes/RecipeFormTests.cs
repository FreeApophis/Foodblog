namespace Foodblog.Test.Recipes;

public sealed class RecipeFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            ingredients: [("Mehl", "200", "1")],
            steps: ["Alles verrühren."]);

        var form = ValidatedAssert.IsSuccess(RecipeForm.Decode(request, Units));

        Assert.Equal(new RecipeName("Rührkuchen"), form.Name);
        Assert.Equal(4, form.Portions);
        Assert.Equal(Difficulty.Easy, form.Difficulty);
        Assert.Single(form.Ingredients);
        Assert.Equal("Mehl", form.Ingredients[0].Name);
        Assert.Equal(200m, form.Ingredients[0].Amount);
        Assert.Single(form.Steps);
    }

    [Fact]
    public void Valid_form_parses_preparation_time_and_cooking_time()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            ingredients: [("Mehl", "200", "1")],
            steps: ["Alles verrühren."],
            prepTime: "10",
            cookTime: "20");

        var form = ValidatedAssert.IsSuccess(RecipeForm.Decode(request, Units));

        Assert.Equal(10, form.PreparationTime);
        Assert.Equal(20, form.CookingTime);
    }

    [Fact]
    public void Missing_times_default_to_zero()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            ingredients: [("Mehl", "200", "1")],
            steps: ["Alles verrühren."]);

        var form = ValidatedAssert.IsSuccess(RecipeForm.Decode(request, Units));

        Assert.Equal(0, form.PreparationTime);
        Assert.Equal(0, form.CookingTime);
    }

    [Fact]
    public void Negative_preparation_time_returns_failure()
    {
        var request = Build("Rührkuchen", "Ein klassischer Rührkuchen.", "4", "0", prepTime: "-5");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.time_invalid", errors);
    }

    [Fact]
    public void Non_numeric_cooking_time_returns_failure()
    {
        var request = Build("Rührkuchen", "Ein klassischer Rührkuchen.", "4", "0", cookTime: "abc");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.time_invalid", errors);
    }

    [Fact]
    public void Name_shorter_than_3_characters_returns_failure()
    {
        var request = Build("AB", "Ein klassischer Rührkuchen.", "4", "0");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.name_too_short", errors);
    }

    [Fact]
    public void Description_shorter_than_10_characters_returns_failure()
    {
        var request = Build("Rührkuchen", "Zu kurz", "4", "0");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.description_too_short", errors);
    }

    [Fact]
    public void Portions_zero_returns_failure()
    {
        var request = Build("Rührkuchen", "Ein klassischer Rührkuchen.", "0", "0");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.portions_invalid", errors);
    }

    [Fact]
    public void Portions_not_a_number_returns_failure()
    {
        var request = Build("Rührkuchen", "Ein klassischer Rührkuchen.", "abc", "0");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.portions_invalid", errors);
    }

    [Fact]
    public void Invalid_difficulty_returns_failure()
    {
        var request = Build("Rührkuchen", "Ein klassischer Rührkuchen.", "4", "99");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.difficulty_invalid", errors);
    }

    [Fact]
    public void No_steps_returns_failure()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            steps: []);

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.no_steps", errors);
    }

    [Fact]
    public void Only_whitespace_steps_return_failure()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            steps: ["   "]);

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.no_steps", errors);
    }

    [Fact]
    public void Invalid_ingredient_row_returns_failure()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            ingredients: [("Mehl", "abc", "g")],
            steps: ["Backen."]);

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.ingredient_invalid", errors);
    }

    [Fact]
    public void Empty_ingredient_rows_are_ignored()
    {
        var request = Build(
            "Rührkuchen",
            "Ein klassischer Rührkuchen.",
            "4",
            "0",
            ingredients: [(string.Empty, string.Empty, "g")],
            steps: ["Backen."]);

        var form = ValidatedAssert.IsSuccess(RecipeForm.Decode(request, Units));

        Assert.Empty(form.Ingredients);
    }

    [Fact]
    public void Multiple_errors_accumulate()
    {
        var request = Build("AB", "Zu kurz", "0", "99");

        var errors = ValidatedAssert.IsFailure(RecipeForm.Decode(request, Units));

        Assert.Contains("recipe.error.name_too_short", errors);
        Assert.Contains("recipe.error.description_too_short", errors);
        Assert.Contains("recipe.error.portions_invalid", errors);
        Assert.Contains("recipe.error.difficulty_invalid", errors);
    }

    [Fact]
    public void ParseIngredients_returns_all_indexed_rows()
    {
        var request = Build(
            "X",
            "X",
            "1",
            "0",
            ingredients: [("Mehl", "100", "g"), ("Zucker", "1.5", "EL")]);

        var ings = RecipeForm.ParseIngredients(request);

        Assert.Equal(2, ings.Count);
        Assert.Equal(("Mehl", "100", "g"), ings[0]);
        Assert.Equal(("Zucker", "1.5", "EL"), ings[1]);
    }

    [Fact]
    public void ParseRawSteps_returns_all_indexed_steps_including_empty()
    {
        var request = Build(
            "X",
            "X",
            "1",
            "0",
            steps: ["Erster Schritt.", string.Empty, "Dritter Schritt."]);

        var steps = RecipeForm.ParseRawSteps(request);

        Assert.Equal(3, steps.Count);
        Assert.Equal("Erster Schritt.", steps[0]);
        Assert.Equal(string.Empty, steps[1]);
        Assert.Equal("Dritter Schritt.", steps[2]);
    }

    private static Request Build(
        string name,
        string description,
        string portions,
        string difficulty,
        IReadOnlyList<(string Name, string Amount, string Unit)>? ingredients = null,
        IReadOnlyList<string>? steps = null,
        string? prepTime = null,
        string? cookTime = null)
    {
        var form = new Dictionary<string, string>
        {
            ["name"] = name,
            ["description"] = description,
            ["portions"] = portions,
            ["difficulty"] = difficulty,
        };

        if (prepTime is not null)
        {
            form["preparation_time"] = prepTime;
        }

        if (cookTime is not null)
        {
            form["cooking_time"] = cookTime;
        }

        foreach (var (i, (ingName, amount, unit)) in (ingredients ?? []).Select((x, i) => (i, x)))
        {
            form[$"ingredient_name_{i}"] = ingName;
            form[$"ingredient_amount_{i}"] = amount;
            form[$"ingredient_unit_{i}"] = unit;
        }

        foreach (var (i, step) in (steps ?? []).Select((s, i) => (i, s)))
        {
            form[$"step_{i}"] = step;
        }

        return new Request(HttpMethod.Post, "/recipes", Empty, Empty, form, Empty);
    }

    private static readonly IReadOnlyList<Unit> Units =
        [Gram, Kilogram, Milliliter, Liter, Deciliter, Tablespoon, Teaspoon, Pinch, Piece];

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
