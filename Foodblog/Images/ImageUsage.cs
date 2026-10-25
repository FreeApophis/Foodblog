using System.Text.RegularExpressions;

namespace Foodblog.Images;

// Finds library images that nothing references, so the admin housekeeping page can offer to remove
// them. An image is referenced either by a typed id (an article cover or a user avatar) or by a
// "/images/{id}" URL appearing anywhere in entity text — inline BBCode ([img]/images/5[/img]),
// recipe image URLs, ingredient images, etc. Scanning the URL form catches every embed regardless
// of which field or markup it lives in, so a still-used image is never reported as an orphan.
public static partial class ImageUsage
{
    public static IReadOnlySet<int> ReferencedIds(
        IEnumerable<Article> articles,
        IEnumerable<Page> pages,
        IEnumerable<Recipe> recipes,
        IEnumerable<Ingredient> ingredients,
        IEnumerable<User> users)
    {
        var ids = new HashSet<int>();

        foreach (var article in articles)
        {
            if (article.CoverImageId is [var cover])
            {
                ids.Add(cover.Value);
            }

            AddUrlIds(ids, article.Teaser.Value);
            AddUrlIds(ids, article.Text.Value);
        }

        foreach (var user in users)
        {
            if (user.AvatarImageId is [var avatar])
            {
                ids.Add(avatar.Value);
            }
        }

        foreach (var page in pages)
        {
            AddUrlIds(ids, page.Content.Value);
        }

        foreach (var recipe in recipes)
        {
            AddUrlIds(ids, recipe.Description.Value);

            foreach (var image in recipe.Images)
            {
                AddUrlIds(ids, image);
            }

            foreach (var step in recipe.PreparationSteps)
            {
                AddUrlIds(ids, step.Text);
            }

            foreach (var hint in recipe.Hints)
            {
                AddUrlIds(ids, hint.Text);
            }
        }

        foreach (var ingredient in ingredients)
        {
            AddUrlIds(ids, ingredient.Image);
            AddUrlIds(ids, ingredient.Description);
        }

        return ids;
    }

    public static IReadOnlyList<ImageSummary> Orphans(
        IEnumerable<ImageSummary> library,
        IReadOnlySet<int> referencedIds) =>
        library.Where(image => !referencedIds.Contains(image.Id.Value)).ToList();

    private static void AddUrlIds(HashSet<int> ids, string text)
    {
        foreach (Match match in ImageUrl().Matches(text ?? string.Empty))
        {
            ids.Add(int.Parse(match.Groups[1].Value));
        }
    }

    [GeneratedRegex(@"/images/(\d+)")]
    private static partial Regex ImageUrl();
}
