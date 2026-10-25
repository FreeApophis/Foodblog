namespace Foodblog.Test.Recipes;

public sealed class InMemoryRecipeRepositoryTests : RecipeRepositoryContract
{
    protected override IRecipeRepository CreateRepository() => new InMemoryRecipeRepository();
}
