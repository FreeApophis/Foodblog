using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Foodblog.Recipes;

// Builds share/SEO metadata for a recipe: Open Graph values plus a schema.org/Recipe JSON-LD
// block (rendered into the document head by Layout). JSON-LD is what lets Google show rich
// recipe cards; the values all come from data the recipe already stores.
public static class RecipeSeo
{
    public static PageMeta Build(
        Recipe recipe,
        IReadOnlyDictionary<IngredientId, Ingredient> ingredients,
        string authorName,
        string baseUrl,
        Translate t,
        string? slug = null)
    {
        var url = $"{baseUrl}/recipes/{slug ?? recipe.Id.Value.ToString()}";
        var imageUrls = recipe.Images.Select(image => Seo.Absolute(baseUrl, image)).ToList();
        var description = Seo.PlainTextSnippet(recipe.Description.Value);

        var data = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Recipe",
            ["name"] = recipe.Name.Value,
            ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = authorName },
            ["url"] = url,
            ["recipeYield"] = recipe.Portions.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrEmpty(description))
        {
            data["description"] = description;
        }

        if (imageUrls.Count > 0)
        {
            data["image"] = imageUrls;
        }

        if (recipe.Tags.Count > 0)
        {
            data["keywords"] = string.Join(", ", recipe.Tags.Select(tag => tag.Value));
        }

        if (recipe.Ingredients.Count > 0)
        {
            data["recipeIngredient"] = recipe.Ingredients
                .Select(ri => IngredientLine(ri, ingredients, t))
                .ToList();
        }

        if (recipe.PreparationSteps.Count > 0)
        {
            data["recipeInstructions"] = recipe.PreparationSteps
                .OrderBy(step => step.SortOrder)
                .Select(step => new Dictionary<string, object?> { ["@type"] = "HowToStep", ["text"] = step.Text })
                .ToList();
        }

        if (recipe.PreparationTime > 0)
        {
            data["prepTime"] = IsoDuration(recipe.PreparationTime);
        }

        if (recipe.CookingTime > 0)
        {
            data["cookTime"] = IsoDuration(recipe.CookingTime);
        }

        var totalTime = recipe.PreparationTime + recipe.CookingTime;
        if (totalTime > 0)
        {
            data["totalTime"] = IsoDuration(totalTime);
        }

        if (recipe.CalorificValue > 0)
        {
            data["nutrition"] = new Dictionary<string, object?>
            {
                ["@type"] = "NutritionInformation",
                ["calories"] = $"{recipe.CalorificValue} kcal",
            };
        }

        // System.Text.Json's default encoder escapes <, >, & and non-ASCII to \uXXXX, so the
        // serialized payload is safe to inline inside a <script> element.
        var json = JsonSerializer.Serialize(data);

        return new PageMeta
        {
            Type = "article",
            Url = url,
            ImageUrl = imageUrls.Count > 0 ? imageUrls[0] : string.Empty,
            Description = description,
            HeadExtra = $"<script type=\"application/ld+json\">{json}</script>",
        };
    }

    private static string IngredientLine(
        RecipeIngredient ri,
        IReadOnlyDictionary<IngredientId, Ingredient> ingredients,
        Translate t)
    {
        var name = ingredients.GetValueOrNone(ri.IngredientId) is [var ingredient] ? ingredient.Name.Value : "?";
        var amount = ri.Amount.ToString("0.####", CultureInfo.InvariantCulture);
        return $"{amount} {t(ri.Unit.AbbreviationKey)} {name}".Trim();
    }

    // Minutes → ISO 8601 duration (90 → "PT1H30M", 20 → "PT20M").
    private static string IsoDuration(int minutes)
    {
        var builder = new StringBuilder("PT");
        if (minutes / 60 > 0)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{minutes / 60}H");
        }

        if (minutes % 60 > 0)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{minutes % 60}M");
        }

        return builder.ToString();
    }
}
