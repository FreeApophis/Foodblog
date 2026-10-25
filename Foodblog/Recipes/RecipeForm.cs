using System.Globalization;

namespace Foodblog.Recipes;

public static class RecipeForm
{
    public sealed record Valid(
        RecipeName Name,
        RecipeDescription Description,
        int Portions,
        Difficulty Difficulty,
        IReadOnlyList<RecipeTag> Tags,
        IReadOnlyList<RecipeHint> Hints,
        IReadOnlyList<IngredientLine> Ingredients,
        IReadOnlyList<PreparationStep> Steps,
        int PreparationTime,
        int CookingTime);

    // A validated ingredient row carrying the typed/selected ingredient name. The name is
    // resolved to an IngredientId (creating a new ingredient when unknown) in the handler,
    // where the repository is available — decoding stays pure.
    public sealed record IngredientLine(string Name, decimal Amount, Foodblog.Domain.Recipes.Unit Unit);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request, IReadOnlyList<Unit> units)
    {
        var name = request.Form.GetValueOrNone("name").GetOrElse(string.Empty).Trim();
        var description = request.Form.GetValueOrNone("description").GetOrElse(string.Empty).Trim();
        var portionsRaw = request.Form.GetValueOrNone("portions").GetOrElse(string.Empty).Trim();
        var difficultyRaw = request.Form.GetValueOrNone("difficulty").GetOrElse(string.Empty).Trim();
        var tagsRaw = request.Form.GetValueOrNone("tags").GetOrElse(string.Empty).Trim();
        var hintsRaw = request.Form.GetValueOrNone("hints").GetOrElse(string.Empty).Trim();
        var prepTimeRaw = request.Form.GetValueOrNone("preparation_time").GetOrElse(string.Empty).Trim();
        var cookTimeRaw = request.Form.GetValueOrNone("cooking_time").GetOrElse(string.Empty).Trim();

        var rawIngredients = ParseIngredients(request);
        var rawSteps = ParseRawSteps(request);

        var tags = ParseTags(tagsRaw);
        var hints = ParseHints(hintsRaw);

        Func<RecipeName, RecipeDescription, int, Difficulty, IReadOnlyList<IngredientLine>, IReadOnlyList<PreparationStep>, int, int, Valid> create =
            (n, d, p, diff, ings, steps, prep, cook) => new Valid(n, d, p, diff, tags, hints, ings, steps, prep, cook);

        return create
            .Apply(TryParseName(name), Combine)
            .Apply(TryParseDescription(description), Combine)
            .Apply(TryParsePortions(portionsRaw), Combine)
            .Apply(TryParseDifficulty(difficultyRaw), Combine)
            .Apply(TryParseIngredients(rawIngredients, units), Combine)
            .Apply(TryParseSteps(rawSteps), Combine)
            .Apply(TryParseTime(prepTimeRaw), Combine)
            .Apply(TryParseTime(cookTimeRaw), Combine);
    }

    public static List<(string Name, string Amount, string Unit)> ParseIngredients(Request request)
    {
        var result = new List<(string, string, string)>();
        for (var i = 0; request.Form.ContainsKey($"ingredient_name_{i}"); i++)
        {
            result.Add((
                request.Form.GetValueOrDefault($"ingredient_name_{i}", string.Empty).Trim(),
                request.Form.GetValueOrDefault($"ingredient_amount_{i}", string.Empty),
                request.Form.GetValueOrDefault($"ingredient_unit_{i}", string.Empty)));
        }

        return result;
    }

    // Reconstructs the recipe's existing image URLs that the editor chose to keep:
    // each is carried in a hidden `existing_image_{i}` field and dropped when its
    // matching `remove_image_{i}` checkbox is ticked.
    public static List<string> ParseKeptImages(Request request)
    {
        var result = new List<string>();
        for (var i = 0; request.Form.ContainsKey($"existing_image_{i}"); i++)
        {
            var url = request.Form.GetValueOrDefault($"existing_image_{i}", string.Empty);
            var removed = request.Form.GetValueOrDefault($"remove_image_{i}", string.Empty) == "on";
            if (!removed && url.Length > 0)
            {
                result.Add(url);
            }
        }

        return result;
    }

    public static List<string> ParseRawSteps(Request request)
    {
        var result = new List<string>();
        for (var i = 0; request.Form.ContainsKey($"step_{i}"); i++)
        {
            result.Add(request.Form[$"step_{i}"].Trim());
        }

        return result;
    }

    private static Validated<IReadOnlyList<string>, RecipeName> TryParseName(string name) =>
        name.Length >= 3
            ? Validated.Succeed<IReadOnlyList<string>, RecipeName>(new RecipeName(name))
            : Validated.Fail<IReadOnlyList<string>, RecipeName>(["recipe.error.name_too_short"]);

    private static Validated<IReadOnlyList<string>, RecipeDescription> TryParseDescription(string description) =>
        description.Length >= 10
            ? Validated.Succeed<IReadOnlyList<string>, RecipeDescription>(new RecipeDescription(description))
            : Validated.Fail<IReadOnlyList<string>, RecipeDescription>(["recipe.error.description_too_short"]);

    private static Validated<IReadOnlyList<string>, int> TryParsePortions(string portionsRaw) =>
        int.TryParse(portionsRaw, out var portions) && portions >= 1
            ? Validated.Succeed<IReadOnlyList<string>, int>(portions)
            : Validated.Fail<IReadOnlyList<string>, int>(["recipe.error.portions_invalid"]);

    // A blank field means "unknown" and decodes to 0; a present value must be a non-negative integer.
    private static Validated<IReadOnlyList<string>, int> TryParseTime(string raw) =>
        raw.Length == 0
            ? Validated.Succeed<IReadOnlyList<string>, int>(0)
            : int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0
                ? Validated.Succeed<IReadOnlyList<string>, int>(value)
                : Validated.Fail<IReadOnlyList<string>, int>(["recipe.error.time_invalid"]);

    private static Validated<IReadOnlyList<string>, Difficulty> TryParseDifficulty(string difficultyRaw) =>
        int.TryParse(difficultyRaw, out var difficultyInt) && Enum.IsDefined(typeof(Difficulty), difficultyInt)
            ? Validated.Succeed<IReadOnlyList<string>, Difficulty>((Difficulty)difficultyInt)
            : Validated.Fail<IReadOnlyList<string>, Difficulty>(["recipe.error.difficulty_invalid"]);

    private static Validated<IReadOnlyList<string>, IReadOnlyList<IngredientLine>> TryParseIngredients(
        List<(string Name, string Amount, string Unit)> ingredients,
        IReadOnlyList<Unit> units)
    {
        var result = new List<IngredientLine>();
        foreach (var (name, amount, unit) in ingredients)
        {
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (!decimal.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) || amt <= 0
                || !int.TryParse(unit, out var unitId)
                || units.FirstOrNone(u => u.Id.Value == unitId) is not [var parsedUnit])
            {
                return Validated.Fail<IReadOnlyList<string>, IReadOnlyList<IngredientLine>>(["recipe.error.ingredient_invalid"]);
            }

            result.Add(new IngredientLine(name, amt, parsedUnit));
        }

        return Validated.Succeed<IReadOnlyList<string>, IReadOnlyList<IngredientLine>>(result);
    }

    private static Validated<IReadOnlyList<string>, IReadOnlyList<PreparationStep>> TryParseSteps(List<string> rawSteps)
    {
        var nonEmpty = rawSteps.Where(s => s.Length > 0).ToList();

        return nonEmpty.Count > 0
            ? Validated.Succeed<IReadOnlyList<string>, IReadOnlyList<PreparationStep>>(
                nonEmpty.Select((text, i) => new PreparationStep(i + 1, text)).ToList())
            : Validated.Fail<IReadOnlyList<string>, IReadOnlyList<PreparationStep>>(["recipe.error.no_steps"]);
    }

    private static IReadOnlyList<RecipeTag> ParseTags(string tagsRaw) =>
        string.IsNullOrWhiteSpace(tagsRaw)
            ? []
            : tagsRaw.Split(',')
                .Select(tag => new RecipeTag(tag.Trim()))
                .Where(tag => tag.Value.Length > 0)
                .ToList();

    private static IReadOnlyList<RecipeHint> ParseHints(string hintsRaw) =>
        string.IsNullOrWhiteSpace(hintsRaw)
            ? []
            : hintsRaw.Split('\n')
                .Select(h => h.Trim())
                .Where(h => h.Length > 0)
                .Select(h => new RecipeHint(h))
                .ToList();

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
