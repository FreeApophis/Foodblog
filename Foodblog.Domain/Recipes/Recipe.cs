namespace Foodblog.Domain.Recipes;

public sealed record Recipe(
    RecipeId Id,
    RecipeName Name,
    RecipeDescription Description,
    IReadOnlyList<PreparationStep> PreparationSteps,
    UserId AuthorId,
    Difficulty Difficulty,
    IReadOnlyList<RecipeTag> Tags,
    int Portions,
    IReadOnlyList<RecipeIngredient> Ingredients,
    IReadOnlyList<string> Images,
    IReadOnlyList<RecipeHint> Hints,
    int PreparationTime,
    int CookingTime,
    int CalorificValue)
{
    public static Recipe Create(
        RecipeId id,
        RecipeName name,
        RecipeDescription description,
        IReadOnlyList<PreparationStep> preparationSteps,
        UserId authorId,
        Difficulty difficulty,
        IReadOnlyList<RecipeTag> tags,
        int portions,
        IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyList<string> images,
        IReadOnlyList<RecipeHint> hints,
        int preparationTime = 0,
        int cookingTime = 0,
        int calorificValue = 0) =>
        new(id, name, description, preparationSteps, authorId, difficulty, tags, portions, ingredients, images, hints, preparationTime, cookingTime, calorificValue);

    public bool Equals(Recipe? other) =>
        other is not null &&
        Id == other.Id &&
        Name == other.Name &&
        Description == other.Description &&
        PreparationSteps.SequenceEqual(other.PreparationSteps) &&
        AuthorId == other.AuthorId &&
        Difficulty == other.Difficulty &&
        Tags.SequenceEqual(other.Tags) &&
        Portions == other.Portions &&
        Ingredients.SequenceEqual(other.Ingredients) &&
        Images.SequenceEqual(other.Images) &&
        Hints.SequenceEqual(other.Hints) &&
        PreparationTime == other.PreparationTime &&
        CookingTime == other.CookingTime &&
        CalorificValue == other.CalorificValue;

    public override int GetHashCode() => Id.GetHashCode();
}
