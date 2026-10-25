namespace Foodblog.Pages;

// Builds share/SEO metadata for a static content page: Open Graph values and a meta description
// drawn from the page body. Generic pages get no structured data — just a clean share card.
public static class PageSeo
{
    public static PageMeta Build(Page page, string baseUrl, string? slug = null) =>
        new()
        {
            Type = "website",
            Url = $"{baseUrl}/pages/{slug ?? page.Id.Value.ToString()}",
            Description = Seo.PlainTextSnippet(page.Content.Value),
        };
}
