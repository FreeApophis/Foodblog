using System.Globalization;
using System.Text.Json;

namespace Foodblog.Articles;

// Builds share/SEO metadata for a blog article: Open Graph values plus a schema.org/BlogPosting
// JSON-LD block. The teaser doubles as the meta description.
public static class ArticleSeo
{
    public static PageMeta Build(Article article, string authorName, string baseUrl, string? slug = null)
    {
        var url = $"{baseUrl}/articles/{slug ?? article.Id.Value.ToString()}";
        var description = Seo.PlainTextSnippet(article.Teaser.Value);
        var imageUrl = article.CoverImageId.Match(
            none: string.Empty,
            some: id => Seo.Absolute(baseUrl, $"/images/{id.Value}"));

        var data = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = article.Title.Value,
            ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = authorName },
            ["datePublished"] = article.PublishedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
            ["mainEntityOfPage"] = url,
            ["url"] = url,
        };

        if (!string.IsNullOrEmpty(description))
        {
            data["description"] = description;
        }

        if (!string.IsNullOrEmpty(imageUrl))
        {
            data["image"] = new[] { imageUrl };
        }

        var json = JsonSerializer.Serialize(data);

        return new PageMeta
        {
            Type = "article",
            Url = url,
            ImageUrl = imageUrl,
            Description = description,
            HeadExtra = $"<script type=\"application/ld+json\">{json}</script>",
        };
    }
}
