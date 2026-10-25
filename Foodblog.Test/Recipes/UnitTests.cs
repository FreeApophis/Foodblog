namespace Foodblog.Test.Recipes;

public sealed class UnitTests
{
    [Fact]
    public void Gram_is_a_weight_unit()
    {
        Assert.Equal(UnitCategory.Weight, Gram.Category);
    }

    [Fact]
    public void Milliliter_is_a_volume_unit()
    {
        Assert.Equal(UnitCategory.Volume, Milliliter.Category);
    }

    [Fact]
    public void Piece_is_a_piece_unit()
    {
        Assert.Equal(UnitCategory.Piece, Piece.Category);
    }

    [Fact]
    public void Gram_name_and_abbreviation_are_translation_keys()
    {
        Assert.Equal("unit.1.name", Gram.NameKey);
        Assert.Equal("unit.1.abbr", Gram.AbbreviationKey);
    }

    [Fact]
    public void ConvertAmount_grams_to_kilograms()
    {
        Assert.Equal(0.25m, Foodblog.Domain.Recipes.Unit.ConvertAmount(250m, Gram, Kilogram));
    }

    [Fact]
    public void ConvertAmount_kilograms_to_grams()
    {
        Assert.Equal(500m, Foodblog.Domain.Recipes.Unit.ConvertAmount(0.5m, Kilogram, Gram));
    }

    [Fact]
    public void ConvertAmount_liters_to_milliliters()
    {
        Assert.Equal(400m, Foodblog.Domain.Recipes.Unit.ConvertAmount(0.4m, Liter, Milliliter));
    }

    [Fact]
    public void ConvertAmount_tablespoons_to_milliliters()
    {
        Assert.Equal(30m, Foodblog.Domain.Recipes.Unit.ConvertAmount(2m, Tablespoon, Milliliter));
    }

    [Fact]
    public void ConvertAmount_across_categories_throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Foodblog.Domain.Recipes.Unit.ConvertAmount(1m, Gram, Liter));
    }

    [Fact]
    public void Units_with_same_values_are_equal()
    {
        var a = new Foodblog.Domain.Recipes.Unit(new UnitId(42), "unit.42.name", "unit.42.abbr", UnitCategory.Weight, 1m);
        var b = new Foodblog.Domain.Recipes.Unit(new UnitId(42), "unit.42.name", "unit.42.abbr", UnitCategory.Weight, 1m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Units_with_different_category_are_not_equal()
    {
        var weight = new Foodblog.Domain.Recipes.Unit(new UnitId(42), "k.name", "k.abbr", UnitCategory.Weight, 1m);
        var volume = new Foodblog.Domain.Recipes.Unit(new UnitId(42), "k.name", "k.abbr", UnitCategory.Volume, 1m);

        Assert.NotEqual(weight, volume);
    }
}
