namespace Foodblog.Test.Recipes;

public sealed class InMemoryIngredientRepositoryTests : IngredientRepositoryContract
{
    protected override IIngredientRepository CreateRepository() => new InMemoryIngredientRepository();
}
