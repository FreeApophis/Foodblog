namespace Foodblog.Recipes;

// Everything the recipe-PDF layouts need, fully resolved by the handler (author name, localized unit
// abbreviations, difficulty label, cover image bytes) so the document itself stays free of repositories.
public sealed record RecipePdfModel(
    string Title,
    string Eyebrow,
    string AuthorName,
    string DifficultyLabel,
    int PreparationTime,
    int CookingTime,
    int Calories,
    int Portions,
    IReadOnlyList<RecipePdfIngredient> Ingredients,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Tips,
    byte[]? CoverImage,
    FocalPoint CoverFocal);

public sealed record RecipePdfIngredient(string Amount, string Name);
