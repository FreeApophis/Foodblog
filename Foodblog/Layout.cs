namespace Foodblog;

public static class Layout
{
    public static string Page(string title, HtmlString body, ViewContext ctx, PageMeta? meta = null)
    {
        return $$"""
        <!doctype html>
        <html lang="de" data-theme="{{Html.Encode(ctx.Theme)}}">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>{{Html.Encode(ctx.SiteName)}} - {{Html.Encode(title)}}</title>
            {{MetaTags(title, meta, ctx.SiteName)}}
            <link rel="stylesheet" href="/styles.css" />
            <link rel='icon' type='image/x-icon' href='/favicon.ico'>
            <link rel='icon' type='image/png' href='/favicon.png'>
            <script src="/htmx.min.js" defer></script>
            <script src="/combobox-keys.js" defer></script>
            <script src="/confirm-delete.js" defer></script>
            <script src="/autofocus-swap.js" defer></script>
            <script src="/focal-point.js" defer></script>
            <script src="/select-on-click.js" defer></script>
        </head>
        <body>
            {{NavViews.UtilityBar(ctx)}}
            {{NavViews.Masthead(ctx)}}
            <main>{{body.Render()}}</main>
            {{NavViews.Footer(ctx)}}
        </body>
        </html>
        """;
    }

    public static string Page(string title, HtmlString body) =>
        Page(title, body, ViewContext.ForGuest());

    // Open Graph / Twitter share-card tags, the canonical link, and any structured-data head
    // extra (e.g. JSON-LD). Rendered only when a PageMeta is supplied; each field is optional
    // and skipped when empty. HeadExtra is emitted verbatim (its producer must keep it safe).
    private static string MetaTags(string title, PageMeta? meta, string siteName)
    {
        if (meta is null)
        {
            return string.Empty;
        }

        var twitterCard = string.IsNullOrEmpty(meta.ImageUrl) ? "summary" : "summary_large_image";

        var tags = new List<string>
        {
            $"""<meta property="og:type" content="{Html.Encode(meta.Type)}" />""",
            $"""<meta property="og:title" content="{Html.Encode(title)}" />""",
            $"""<meta property="og:site_name" content="{Html.Encode(siteName)}" />""",
            $"""<meta name="twitter:card" content="{twitterCard}" />""",
            $"""<meta name="twitter:title" content="{Html.Encode(title)}" />""",
        };

        if (!string.IsNullOrEmpty(meta.Description))
        {
            tags.Add($"""<meta name="description" content="{Html.Encode(meta.Description)}" />""");
            tags.Add($"""<meta property="og:description" content="{Html.Encode(meta.Description)}" />""");
            tags.Add($"""<meta name="twitter:description" content="{Html.Encode(meta.Description)}" />""");
        }

        if (!string.IsNullOrEmpty(meta.Url))
        {
            tags.Add($"""<meta property="og:url" content="{Html.Encode(meta.Url)}" />""");
            tags.Add($"""<link rel="canonical" href="{Html.Encode(meta.Url)}" />""");
        }

        if (!string.IsNullOrEmpty(meta.ImageUrl))
        {
            tags.Add($"""<meta property="og:image" content="{Html.Encode(meta.ImageUrl)}" />""");
            tags.Add($"""<meta name="twitter:image" content="{Html.Encode(meta.ImageUrl)}" />""");
        }

        if (!string.IsNullOrEmpty(meta.HeadExtra))
        {
            tags.Add(meta.HeadExtra);
        }

        return string.Join("\n            ", tags);
    }
}
