namespace Foodblog.Domain.Recipes;

public sealed record Ingredient(
    IngredientId Id,
    IngredientName Name,
    string Image,
    string Description,
    decimal Density,
    decimal PieceCount,
    decimal CalorificValue,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates,
    decimal Sugar,
    decimal Fiber)
{
    // True when descriptive fields nobody can default sensibly are still blank — used by the admin
    // overview to flag stub ingredients (e.g. those quick-created from the recipe form) that still
    // need filling in. Numeric fields are excluded because zero can be a legitimate value.
    public bool HasMissingInformation =>
        string.IsNullOrWhiteSpace(Description) || string.IsNullOrWhiteSpace(Image);

    public static Ingredient Create(
        IngredientId id,
        IngredientName name,
        string image,
        string description,
        decimal density,
        decimal pieceCount,
        decimal calorificValue,
        decimal protein,
        decimal fat,
        decimal carbohydrates,
        decimal sugar,
        decimal fiber) =>
        new(id, name, image, description, density, pieceCount, calorificValue, protein, fat, carbohydrates, sugar, fiber);
}
