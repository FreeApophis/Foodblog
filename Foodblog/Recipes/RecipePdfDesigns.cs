namespace Foodblog.Recipes;

public static class RecipePdfDesigns
{
    // Parses the ?design= query value; anything that is not the alternative falls back to the default.
    public static RecipePdfDesign Parse(string? raw) =>
        string.Equals(raw, "alternative", StringComparison.OrdinalIgnoreCase)
            ? RecipePdfDesign.Alternative
            : RecipePdfDesign.Default;
}
