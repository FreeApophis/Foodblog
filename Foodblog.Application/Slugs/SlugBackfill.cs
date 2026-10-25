namespace Foodblog.Application.Slugs;

// Ensures every existing entity has a slug in the registry. Runs at startup and is idempotent
// (SlugService.Ensure keeps an entity's existing slug), so it safely picks up any rows created
// before the slug write-path existed. Content types are processed before tags, so on a slug
// collision the content entity keeps the bare slug and the tag is the one that gets suffixed.
public static class SlugBackfill
{
    public static async Task Run(
        SlugService slugs,
        IArticleRepository articles,
        IRecipeRepository recipes,
        IPageRepository pages,
        IIngredientRepository ingredients,
        ITagRepository tags)
    {
        foreach (var article in await articles.All())
        {
            await slugs.Ensure(SlugEntityType.Article, article.Id.Value, article.Title.Value);
        }

        foreach (var recipe in await recipes.All())
        {
            await slugs.Ensure(SlugEntityType.Recipe, recipe.Id.Value, recipe.Name.Value);
        }

        foreach (var page in await pages.All())
        {
            await slugs.Ensure(SlugEntityType.Page, page.Id.Value, page.Title.Value);
        }

        foreach (var ingredient in await ingredients.All())
        {
            await slugs.Ensure(SlugEntityType.Ingredient, ingredient.Id.Value, ingredient.Name.Value);
        }

        foreach (var tag in await tags.All())
        {
            await slugs.Ensure(SlugEntityType.Tag, tag.Id, tag.Name);
        }
    }
}
