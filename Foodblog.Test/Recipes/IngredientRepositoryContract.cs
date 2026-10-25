namespace Foodblog.Test.Recipes;

public abstract class IngredientRepositoryContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_ingredient()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var ingredient = AnIngredient(id);

        await repo.Save(ingredient);

        Assert.Equal(Option.Some(ingredient), await repo.Find(id));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_id()
    {
        var repo = CreateRepository();

        FunctionalAssert.None(await repo.Find(new IngredientId(987_654)));
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
    public async Task All_returns_all_saved_ingredients()
    {
        var repo = CreateRepository();
        var first = AnIngredient(await repo.NextId(), name: "Mehl");
        var second = AnIngredient(await repo.NextId(), name: "Zucker");

        await repo.Save(first);
        await repo.Save(second);

        var all = await repo.All();
        Assert.Contains(first, all);
        Assert.Contains(second, all);
    }

    [Fact]
    public async Task All_returns_ingredients_sorted_alphabetically_ignoring_case()
    {
        var repo = CreateRepository();

        // Saved out of order; the lowercase "alpha" pins case-insensitivity (a case-sensitive sort
        // would push a lowercase name behind the uppercase ones). Relative order is asserted so the
        // test holds even when the store already contains other (e.g. seeded) ingredients.
        await repo.Save(AnIngredient(await repo.NextId(), name: "Zebra"));
        await repo.Save(AnIngredient(await repo.NextId(), name: "alpha"));
        await repo.Save(AnIngredient(await repo.NextId(), name: "Mango"));

        var names = (await repo.All()).Select(i => i.Name.Value).ToList();

        Assert.True(names.IndexOf("alpha") < names.IndexOf("Mango"));
        Assert.True(names.IndexOf("Mango") < names.IndexOf("Zebra"));
    }

    [Fact]
    public async Task Save_replaces_an_existing_ingredient_with_the_same_id()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        var original = AnIngredient(id, name: "Original");
        var updated = AnIngredient(id, name: "Aktualisiert");

        await repo.Save(original);
        await repo.Save(updated);

        Assert.Equal(Option.Some(updated), await repo.Find(id));
    }

    [Fact]
    public async Task Delete_removes_the_ingredient()
    {
        var repo = CreateRepository();
        var id = await repo.NextId();
        await repo.Save(AnIngredient(id));

        await repo.Delete(id);

        FunctionalAssert.None(await repo.Find(id));
    }

    [Fact]
    public async Task Delete_is_idempotent_for_unknown_id()
    {
        var repo = CreateRepository();

        await repo.Delete(new IngredientId(987_654));
    }

    protected abstract IIngredientRepository CreateRepository();

    private static Ingredient AnIngredient(IngredientId id, string name = "Zutat") =>
        Ingredient.Create(
            id,
            new IngredientName(name),
            string.Empty,
            "Eine Beschreibung",
            density: 1.0m,
            pieceCount: 1.0m,
            calorificValue: 100m,
            protein: 5m,
            fat: 3m,
            carbohydrates: 20m,
            sugar: 5m,
            fiber: 2m);
}
