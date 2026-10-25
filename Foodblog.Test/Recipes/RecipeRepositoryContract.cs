namespace Foodblog.Test.Recipes;

public abstract class RecipeRepositoryContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_recipe()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var recipe = ARecipe(id);

        await repo.Save(recipe);

        Assert.Equal(Option.Some(recipe), await repo.Find(id));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_id()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.Find(new RecipeId(987_654)));
    }

    [Fact]
    public async Task NextId_returns_an_id_that_does_not_yet_exist()
    {
        var repo = CreateRepository();

        var id = await repo.NextId();

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task NextId_returns_distinct_values_across_calls()
    {
        var repo = CreateRepository();

        var first = await repo.NextId();
        var second = await repo.NextId();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task All_returns_all_saved_recipes()
    {
        var repo = CreateRepository();
        var first = ARecipe(await repo.NextId(), name: "Pasta");
        var second = ARecipe(await repo.NextId(), name: "Pizza");

        await repo.Save(first);
        await repo.Save(second);

        var all = await repo.All();
        Assert.Contains(first, all);
        Assert.Contains(second, all);
    }

    [Fact]
    public async Task Save_replaces_an_existing_recipe_with_the_same_id()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var original = ARecipe(id, name: "Original");
        var updated = ARecipe(id, name: "Aktualisiert");

        await repo.Save(original);
        await repo.Save(updated);

        Assert.Equal(Option.Some(updated), await repo.Find(id));
    }

    [Fact]
    public async Task Delete_removes_the_recipe()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        await repo.Save(ARecipe(id));

        await repo.Delete(id);

        Assert.Equal(Option<Recipe>.None, await repo.Find(id));
    }

    [Fact]
    public async Task Delete_is_idempotent_for_unknown_id()
    {
        var repo = CreateRepository();

        await repo.Delete(new RecipeId(987_654));
    }

    [Fact]
    public async Task UpdateCalorificValue_changes_only_the_calorie_field()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var recipe = ARecipe(id, name: "Suppe") with { CalorificValue = 100 };
        await repo.Save(recipe);

        await repo.UpdateCalorificValue(id, 250);

        var found = FunctionalAssert.Some(await repo.Find(id));
        Assert.Equal(250, found.CalorificValue);
        Assert.Equal(new RecipeName("Suppe"), found.Name);
    }

    [Fact]
    public async Task Save_then_Find_round_trips_preparation_and_cooking_time_and_calories()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var recipe = ARecipe(id) with { PreparationTime = 10, CookingTime = 20, CalorificValue = 227 };

        await repo.Save(recipe);

        var found = FunctionalAssert.Some(await repo.Find(id));
        Assert.Equal(10, found.PreparationTime);
        Assert.Equal(20, found.CookingTime);
        Assert.Equal(227, found.CalorificValue);
    }

    [Fact]
    public async Task Save_then_Find_round_trips_tags()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var recipe = ARecipe(id) with { Tags = [new RecipeTag("Backen"), new RecipeTag("Kuchen")] };

        await repo.Save(recipe);

        // Tags round-trip, modulo canonical casing: the SQLite impl deduplicates tags into a
        // shared dictionary keyed by a case-folded slug, so the stored display casing may differ.
        var found = FunctionalAssert.Some(await repo.Find(id));
        var tags = found.Tags.Select(t => t.Value.ToLowerInvariant()).ToList();
        Assert.Contains("backen", tags);
        Assert.Contains("kuchen", tags);
    }

    [Fact]
    public async Task FindByTag_returns_only_recipes_carrying_that_tag()
    {
        var repo = CreateRepository();
        var tagged = ARecipe(await repo.NextId(), name: "Getaggt") with { Tags = [new RecipeTag("Süss")] };
        var untagged = ARecipe(await repo.NextId(), name: "Schlicht");
        await repo.Save(tagged);
        await repo.Save(untagged);

        // "Süss" slugifies to "suess".
        var found = await repo.FindByTag("suess");

        Assert.Contains(found, r => r.Id == tagged.Id);
        Assert.DoesNotContain(found, r => r.Id == untagged.Id);
    }

    protected abstract IRecipeRepository CreateRepository();

    private static Recipe ARecipe(RecipeId id, string name = "Rezept") =>
        Recipe.Create(
            id,
            new RecipeName(name),
            new RecipeDescription("Eine Beschreibung"),
            [],
            new UserId(1),
            Difficulty.Medium,
            [],
            2,
            [],
            [],
            []);
}
