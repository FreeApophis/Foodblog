using System.Text;

namespace Foodblog.Test.Recipes;

public sealed class RecipePdfTests
{
    // A valid 1x1 transparent PNG so QuestPDF's image decoder has something real to embed.
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M8AAAMCAoBoPgC+AAAAAElFTkSuQmCC");

    private static readonly Translate Echo = Functional.Identity;

    [Fact]
    public void Generate_default_design_produces_a_pdf()
    {
        var bytes = RecipePdf.Generate(AModel(), RecipePdfDesign.Default, Echo);

        AssertIsPdf(bytes);
    }

    [Fact]
    public void Generate_alternative_design_produces_a_pdf()
    {
        var bytes = RecipePdf.Generate(AModel(), RecipePdfDesign.Alternative, Echo);

        AssertIsPdf(bytes);
    }

    [Fact]
    public void Generate_embeds_a_cover_image()
    {
        var bytes = RecipePdf.Generate(AModel() with { CoverImage = OnePixelPng }, RecipePdfDesign.Default, Echo);

        AssertIsPdf(bytes);
    }

    [Fact]
    public void Generate_handles_a_recipe_without_cover_steps_or_tips()
    {
        var sparse = AModel() with { CoverImage = null, Ingredients = [], Steps = [], Tips = [] };

        AssertIsPdf(RecipePdf.Generate(sparse, RecipePdfDesign.Default, Echo));
        AssertIsPdf(RecipePdf.Generate(sparse, RecipePdfDesign.Alternative, Echo));
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static RecipePdfModel AModel() => new(
        Title: "Crispy Chicken mit fruchtiger Currysauce",
        Eyebrow: "SCHNELLES GERICHT",
        AuthorName: "Sabrina",
        DifficultyLabel: "EINFACH",
        PreparationTime: 10,
        CookingTime: 20,
        Calories: 227,
        Portions: 2,
        Ingredients:
        [
            new RecipePdfIngredient("200 g", "Hähnchen-Minutenfilets"),
            new RecipePdfIngredient("100 g", "Mehl"),
            new RecipePdfIngredient("1 St.", "Ei"),
        ],
        Steps:
        [
            "Reis nach Packungsanweisung zubereiten.",
            "Mehl mit Paprika, Salz und Pfeffer vermischen.",
        ],
        Tips: ["Den Fruchtcocktail gut abtropfen lassen."],
        CoverImage: null,
        CoverFocal: FocalPoint.Center);
}
