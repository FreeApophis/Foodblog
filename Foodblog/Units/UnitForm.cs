using System.Globalization;

namespace Foodblog.Units;

public static class UnitForm
{
    public sealed record Valid(string Name, string Abbreviation, UnitCategory Category, decimal Factor);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var name = request.Form.GetValueOrNone("name").GetOrElse(string.Empty).Trim();
        var abbreviation = request.Form.GetValueOrNone("abbreviation").GetOrElse(string.Empty).Trim();
        var category = request.Form.GetValueOrNone("category").GetOrElse(string.Empty).Trim();
        var factor = request.Form.GetValueOrNone("factor").GetOrElse(string.Empty).Trim();

        Func<string, string, UnitCategory, decimal, Valid> create =
            (n, a, c, f) => new Valid(n, a, c, f);

        return create
            .Apply(TryParseName(name), Combine)
            .Apply(TryParseAbbreviation(abbreviation), Combine)
            .Apply(TryParseCategory(category), Combine)
            .Apply(TryParseFactor(factor), Combine);
    }

    private static Validated<IReadOnlyList<string>, string> TryParseName(string name) =>
        name.Length >= 1
            ? Validated.Succeed<IReadOnlyList<string>, string>(name)
            : Validated.Fail<IReadOnlyList<string>, string>(["unit.error.name_required"]);

    private static Validated<IReadOnlyList<string>, string> TryParseAbbreviation(string abbreviation) =>
        abbreviation.Length >= 1
            ? Validated.Succeed<IReadOnlyList<string>, string>(abbreviation)
            : Validated.Fail<IReadOnlyList<string>, string>(["unit.error.abbreviation_required"]);

    private static Validated<IReadOnlyList<string>, UnitCategory> TryParseCategory(string raw) =>
        int.TryParse(raw, out var value) && Enum.IsDefined(typeof(UnitCategory), value)
            ? Validated.Succeed<IReadOnlyList<string>, UnitCategory>((UnitCategory)value)
            : Validated.Fail<IReadOnlyList<string>, UnitCategory>(["unit.error.category_invalid"]);

    private static Validated<IReadOnlyList<string>, decimal> TryParseFactor(string raw) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) && value > 0
            ? Validated.Succeed<IReadOnlyList<string>, decimal>(value)
            : Validated.Fail<IReadOnlyList<string>, decimal>(["unit.error.factor_invalid"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
