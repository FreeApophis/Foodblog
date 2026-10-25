namespace Foodblog.Domain.Recipes;

// A measurement unit, now driven by the database. Name and abbreviation are translation
// keys (resolved through the translation cache); the factor is relative to the category's
// base unit so amounts can be converted within a category.
public sealed record Unit(UnitId Id, string NameKey, string AbbreviationKey, UnitCategory Category, decimal Factor)
{
    // Conversion only makes sense within a category; bases differ per category but only the
    // factor ratio matters. Named ConvertAmount (not Convert) to avoid clashing with
    // System.Convert under the `using static Unit` global import.
    public static decimal ConvertAmount(decimal amount, Unit from, Unit to) =>
        from.Category == to.Category
            ? amount * from.Factor / to.Factor
            : throw new InvalidOperationException("Cannot convert between unit categories.");

    // Well-known instances matching the seeded ids/keys/factors, so the seeder and tests stay terse.
    public static Unit Gram { get; } = new(new UnitId(1), "unit.1.name", "unit.1.abbr", UnitCategory.Weight, 0.001m);

    public static Unit Kilogram { get; } = new(new UnitId(2), "unit.2.name", "unit.2.abbr", UnitCategory.Weight, 1m);

    public static Unit Milliliter { get; } = new(new UnitId(3), "unit.3.name", "unit.3.abbr", UnitCategory.Volume, 0.001m);

    public static Unit Liter { get; } = new(new UnitId(4), "unit.4.name", "unit.4.abbr", UnitCategory.Volume, 1m);

    public static Unit Deciliter { get; } = new(new UnitId(15), "unit.15.name", "unit.15.abbr", UnitCategory.Volume, 0.1m);

    public static Unit Tablespoon { get; } = new(new UnitId(7), "unit.7.name", "unit.7.abbr", UnitCategory.Volume, 0.015m);

    public static Unit Teaspoon { get; } = new(new UnitId(6), "unit.6.name", "unit.6.abbr", UnitCategory.Volume, 0.005m);

    public static Unit Pinch { get; } = new(new UnitId(5), "unit.5.name", "unit.5.abbr", UnitCategory.Volume, 0.000625m);

    public static Unit Piece { get; } = new(new UnitId(9), "unit.9.name", "unit.9.abbr", UnitCategory.Piece, 1m);
}
