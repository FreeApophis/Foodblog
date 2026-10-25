namespace Foodblog.Test.Tags;

public sealed class SlugTests
{
    [Theory]
    [InlineData("süss", "suess")]
    [InlineData("Süss", "suess")]
    [InlineData("Vegetarisch", "vegetarisch")]
    [InlineData("Schweizer Küche", "schweizer-kueche")]
    [InlineData("One-Pot", "one-pot")]
    [InlineData("Größe", "groesse")]
    [InlineData("  süss  ", "suess")]
    [InlineData("Crème brûlée", "creme-brulee")]
    [InlineData("Tiramisù", "tiramisu")]
    [InlineData("Pâté à la française", "pate-a-la-francaise")]
    [InlineData("Café & Crème", "cafe-creme")]
    [InlineData("Gnocchi (selbstgemacht)", "gnocchi-selbstgemacht")]
    [InlineData("Niçoise", "nicoise")]
    [InlineData("Zürich", "zuerich")]
    public void Transliterates_accents_lowercases_and_hyphenates(string input, string expected) =>
        Assert.Equal(expected, Slug.From(input));

    [Theory]
    [InlineData("", "n-a")]
    [InlineData("   ", "n-a")]
    [InlineData("!!!", "n-a")]
    [InlineData("---", "n-a")]
    public void Falls_back_when_nothing_sluggable_remains(string input, string expected) =>
        Assert.Equal(expected, Slug.From(input));
}
