namespace Foodblog.Test.Recipes;

public class RecipeSeoTests
{
    // Stub translator: friendly abbreviation for grams, identity otherwise.
    private static readonly Translate T = key => key == Gram.AbbreviationKey ? "g" : key;

    [Fact]
    public void Build_sets_share_metadata_from_the_recipe()
    {
        var (recipe, ingredients) = Sample();

        var meta = RecipeSeo.Build(recipe, ingredients, "Anna", "https://foodblog.ch", T);

        Assert.Equal("article", meta.Type);
        Assert.Equal("https://foodblog.ch/recipes/7", meta.Url);
        Assert.Equal("https://foodblog.ch/images/7", meta.ImageUrl);
        Assert.Contains("klassischer Kuchen", meta.Description);
        Assert.DoesNotContain("[b]", meta.Description);
    }

    [Fact]
    public void Build_emits_recipe_json_ld_with_core_fields()
    {
        var (recipe, ingredients) = Sample();

        var json = RecipeSeo.Build(recipe, ingredients, "Anna", "https://foodblog.ch", T).HeadExtra;

        Assert.Contains("<script type=\"application/ld+json\">", json);
        Assert.Contains("\"@type\":\"Recipe\"", json);
        Assert.Contains("\"recipeYield\":\"4\"", json);
        Assert.Contains("\"recipeIngredient\":[\"200 g Mehl\"]", json);
        Assert.Contains("\"@type\":\"HowToStep\"", json);
        Assert.Contains("Alles verr", json);
        Assert.Contains("\"prepTime\":\"PT20M\"", json);
        Assert.Contains("\"cookTime\":\"PT1H10M\"", json);
        Assert.Contains("\"totalTime\":\"PT1H30M\"", json);
        Assert.Contains("\"calories\":\"350 kcal\"", json);
        Assert.Contains("https://foodblog.ch/images/7", json);
        Assert.Contains("Dessert", json);
        Assert.Contains("Anna", json);
    }

    [Fact]
    public void Build_uses_the_slug_for_canonical_urls_when_supplied()
    {
        var (recipe, ingredients) = Sample();

        var meta = RecipeSeo.Build(recipe, ingredients, "Anna", "https://foodblog.ch", T, "ruehrkuchen");

        Assert.Equal("https://foodblog.ch/recipes/ruehrkuchen", meta.Url);
        Assert.Contains("\"url\":\"https://foodblog.ch/recipes/ruehrkuchen\"", meta.HeadExtra);
    }

    [Fact]
    public void Build_omits_optional_fields_when_absent()
    {
        var recipe = Recipe.Create(
            new RecipeId(3),
            new RecipeName("Wasser"),
            new RecipeDescription(string.Empty),
            [],
            new UserId(1),
            Difficulty.Easy,
            [],
            portions: 1,
            [],
            [],
            []);

        var json = RecipeSeo.Build(recipe, new Dictionary<IngredientId, Ingredient>(), "Anna", "https://foodblog.ch", T).HeadExtra;

        Assert.DoesNotContain("prepTime", json);
        Assert.DoesNotContain("cookTime", json);
        Assert.DoesNotContain("nutrition", json);
        Assert.DoesNotContain("\"image\"", json);
    }

    private static (Recipe Recipe, IReadOnlyDictionary<IngredientId, Ingredient> Ingredients) Sample()
    {
        var ingredientId = new IngredientId(1);
        var ingredients = new Dictionary<IngredientId, Ingredient>
        {
            [ingredientId] = Ingredient.Create(
                ingredientId,
                new IngredientName("Mehl"),
                image: string.Empty,
                description: string.Empty,
                density: 1m,
                pieceCount: 0m,
                calorificValue: 364m,
                protein: 10m,
                fat: 1m,
                carbohydrates: 76m,
                sugar: 1m,
                fiber: 3m),
        };

        var recipe = Recipe.Create(
            new RecipeId(7),
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein [b]klassischer[/b] Kuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            new UserId(1),
            Difficulty.Easy,
            [new RecipeTag("Dessert")],
            portions: 4,
            [new RecipeIngredient(ingredientId, 200m, Gram)],
            ["/images/7"],
            [],
            preparationTime: 20,
            cookingTime: 70,
            calorificValue: 350);

        return (recipe, ingredients);
    }
}
