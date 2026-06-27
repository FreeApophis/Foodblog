namespace FunctionalBlog.Recipes;

// Maps a recipe to a plain RecipePdfModel, scaling ingredient amounts to the requested serving count
// (calories stay per-serving). Kept separate from the handler so the scaling is unit-testable.
public static class RecipePdfMapper
{
    public static RecipePdfModel Create(
        Recipe recipe,
        int displayPortions,
        string authorName,
        IReadOnlyDictionary<IngredientId, string> ingredientNames,
        byte[]? coverImage,
        Translate t,
        FocalPoint? coverFocal = null)
    {
        var factor = recipe.Portions > 0 ? (decimal)displayPortions / recipe.Portions : 1m;

        var ingredients = recipe.Ingredients.Select(line => new RecipePdfIngredient(
            $"{AmountFormat.Format(line.Amount * factor)} {t(line.Unit.AbbreviationKey)}".Trim(),
            ingredientNames.GetValueOrNone(line.IngredientId).GetOrElse("?"))).ToList();

        var eyebrow = recipe.Tags.Count > 0
            ? recipe.Tags[0].Value.ToUpperInvariant()
            : t("recipe.pdf.eyebrow");

        return new RecipePdfModel(
            Title: recipe.Name.Value,
            Eyebrow: eyebrow,
            AuthorName: authorName,
            DifficultyLabel: t(DifficultyKey(recipe.Difficulty)).ToUpperInvariant(),
            PreparationTime: recipe.PreparationTime,
            CookingTime: recipe.CookingTime,
            Calories: recipe.CalorificValue,
            Portions: displayPortions,
            Ingredients: ingredients,
            Steps: recipe.PreparationSteps.OrderBy(step => step.SortOrder).Select(step => step.Text).ToList(),
            Tips: recipe.Hints.Select(hint => hint.Text).ToList(),
            CoverImage: coverImage,
            CoverFocal: coverFocal ?? FocalPoint.Center);
    }

    private static string DifficultyKey(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => "recipe.difficulty.easy",
        Difficulty.Medium => "recipe.difficulty.medium",
        Difficulty.Hard => "recipe.difficulty.hard",
        _ => difficulty.ToString(),
    };
}
