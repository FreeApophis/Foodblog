using System.Globalization;

namespace Foodblog.Ingredients;

public static class IngredientForm
{
    public sealed record Valid(
        IngredientName Name,
        string Description,
        string Image,
        decimal Density,
        decimal PieceCount,
        decimal CalorificValue,
        decimal Protein,
        decimal Fat,
        decimal Carbohydrates,
        decimal Sugar,
        decimal Fiber);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var name = request.Form.GetValueOrNone("name").GetOrElse(string.Empty).Trim();
        var description = request.Form.GetValueOrNone("description").GetOrElse(string.Empty).Trim();
        var image = request.Form.GetValueOrNone("image").GetOrElse(string.Empty).Trim();
        var density = request.Form.GetValueOrNone("density").GetOrElse(string.Empty).Trim();
        var pieceCount = request.Form.GetValueOrNone("piece_count").GetOrElse(string.Empty).Trim();
        var calorificValue = request.Form.GetValueOrNone("calorific_value").GetOrElse(string.Empty).Trim();
        var protein = request.Form.GetValueOrNone("protein").GetOrElse(string.Empty).Trim();
        var fat = request.Form.GetValueOrNone("fat").GetOrElse(string.Empty).Trim();
        var carbohydrates = request.Form.GetValueOrNone("carbohydrates").GetOrElse(string.Empty).Trim();
        var sugar = request.Form.GetValueOrNone("sugar").GetOrElse(string.Empty).Trim();
        var fiber = request.Form.GetValueOrNone("fiber").GetOrElse(string.Empty).Trim();

        Func<IngredientName, (decimal D, decimal P, decimal C), (decimal Pr, decimal F, decimal Cb, decimal S, decimal Fi), Valid> create =
            (n, physical, nutrients) => new Valid(
                n,
                description,
                image,
                physical.D,
                physical.P,
                physical.C,
                nutrients.Pr,
                nutrients.F,
                nutrients.Cb,
                nutrients.S,
                nutrients.Fi);

        return create
            .Apply(TryParseName(name), Combine)
            .Apply(TryParsePhysical(density, pieceCount, calorificValue), Combine)
            .Apply(TryParseNutrients(protein, fat, carbohydrates, sugar, fiber), Combine);
    }

    private static Validated<IReadOnlyList<string>, IngredientName> TryParseName(string name) =>
        name.Length >= 2
            ? Validated.Succeed<IReadOnlyList<string>, IngredientName>(new IngredientName(name))
            : Validated.Fail<IReadOnlyList<string>, IngredientName>(["ingredient.error.name_too_short"]);

    private static Validated<IReadOnlyList<string>, (decimal Density, decimal PieceCount, decimal CalorificValue)>
        TryParsePhysical(string density, string pieceCount, string calorificValue)
    {
        Func<bool, bool, bool, (decimal, decimal, decimal)> always = (_, _, _) => (
            Parse(density), Parse(pieceCount), Parse(calorificValue));

        return always
            .Apply(CheckPositive(density, "ingredient.error.density_invalid"), Combine)
            .Apply(CheckNonNegative(pieceCount, "ingredient.error.piece_count_invalid"), Combine)
            .Apply(CheckNonNegative(calorificValue, "ingredient.error.calorific_value_invalid"), Combine);
    }

    private static Validated<IReadOnlyList<string>, (decimal Protein, decimal Fat, decimal Carbohydrates, decimal Sugar, decimal Fiber)>
        TryParseNutrients(string protein, string fat, string carbohydrates, string sugar, string fiber)
    {
        Func<bool, bool, bool, bool, bool, (decimal, decimal, decimal, decimal, decimal)> always = (_, _, _, _, _) => (
            Parse(protein), Parse(fat), Parse(carbohydrates), Parse(sugar), Parse(fiber));

        return always
            .Apply(CheckNonNegative(protein, "ingredient.error.protein_invalid"), Combine)
            .Apply(CheckNonNegative(fat, "ingredient.error.fat_invalid"), Combine)
            .Apply(CheckNonNegative(carbohydrates, "ingredient.error.carbohydrates_invalid"), Combine)
            .Apply(CheckNonNegative(sugar, "ingredient.error.sugar_invalid"), Combine)
            .Apply(CheckNonNegative(fiber, "ingredient.error.fiber_invalid"), Combine);
    }

    private static Validated<IReadOnlyList<string>, bool> CheckPositive(string raw, string errorKey) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) && val > 0
            ? Validated.Succeed<IReadOnlyList<string>, bool>(true)
            : Validated.Fail<IReadOnlyList<string>, bool>([errorKey]);

    private static Validated<IReadOnlyList<string>, bool> CheckNonNegative(string raw, string errorKey) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) && val >= 0
            ? Validated.Succeed<IReadOnlyList<string>, bool>(true)
            : Validated.Fail<IReadOnlyList<string>, bool>([errorKey]);

    private static decimal Parse(string raw) =>
        decimal.Parse(raw, NumberStyles.Any, CultureInfo.InvariantCulture);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
