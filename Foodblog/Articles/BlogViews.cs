using System.Globalization;

namespace Foodblog.Articles;

public static class BlogViews
{
    public static string Index(
        IReadOnlyList<Article> articles,
        IReadOnlyDictionary<UserId, string> authorNames,
        IReadOnlyDictionary<UserId, Option<ImageId>> authorAvatars,
        IReadOnlyList<Recipe> recipes,
        ViewContext ctx)
    {
        var (principal, t, _) = ctx;

        string Featured(Article a) =>
            $"""
            <a class="blog-featured" href="{ctx.Url(SlugEntityType.Article, a.Id.Value)}">
                {Media(a, ctx)}
                <div class="blog-featured-body">
                    <h2>{Html.Encode(a.Title.Value)}</h2>
                    <p class="blog-excerpt">{Html.Encode(a.Teaser.Value)}</p>
                    <div class="blog-foot">
                        {Author(a, authorNames, authorAvatars)}
                        <span class="blog-readmore">{Html.Encode(t("blog.read_more"))} →</span>
                    </div>
                </div>
            </a>
            """;

        HtmlString feed;
        if (articles.Count == 0)
        {
            feed = Html.P(Html.Text(t("blog.no_articles")));
        }
        else
        {
            var rest = articles.Skip(1).ToList();
            var grid = rest.Count > 0
                ? Html.Raw($"""<div class="blog-grid">{string.Concat(rest.Select(a => Card(a, authorNames, authorAvatars, ctx)))}</div>""")
                : HtmlString.Empty;
            feed = Html.Raw(Featured(articles[0])) + grid;
        }

        var newButton = principal.Can<Create>(new ArticleResource())
            ? Html.Raw($"""<a class="btn" href="/articles/new">{Html.Encode(t("blog.new_article"))}</a>""")
            : HtmlString.Empty;

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("blog.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(t("blog.title"))}</h1>
            """) + newButton + Html.Raw("</div></div>");

        var layout = Html.Raw("""<div class="blog-layout"><div class="blog-main">""") +
            feed +
            Html.Raw($"""</div><aside class="blog-sidebar">{Sidebar(recipes, ctx)}</aside></div>""");

        var body = head + layout;

        return Layout.Page(t("blog.title"), body, ctx);
    }

    public static string Show(Article article, string authorName, ViewContext ctx, string baseUrl, Option<ImageId> authorAvatar = default)
    {
        var (principal, t, csrfToken) = ctx;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("blog.title"), "/"),
            Crumb.Current(article.Title.Value));

        // Author + date + edit/delete laid out like the recipe detail page: the name carries an
        // avatar, and edit/delete are round icon buttons pushed to the end of the meta row.
        var editButton = principal.Can<Edit>(new ArticleResource())
            ? Html.Raw($"""<a class="icon-round" href="/articles/{article.Id.Value}/edit" title="{Html.Encode(t("common.edit"))}" aria-label="{Html.Encode(t("common.edit"))}">{PencilIcon}</a>""")
            : HtmlString.Empty;

        var deleteIcon = Html.Raw($"""<button type="submit" class="icon-round icon-round-danger" title="{Html.Encode(t("common.delete"))}" aria-label="{Html.Encode(t("common.delete"))}">{TrashIcon}</button>""");
        var deleteButton = principal.Can<Delete>(new ArticleResource())
            ? Html.Form($"/articles/{article.Id.Value}/delete", Html.CsrfField(csrfToken) + deleteIcon, cssClass: "inline-form", confirm: t("common.confirm_delete"))
            : HtmlString.Empty;

        var metaActions = Html.Raw("""<span class="recipe-meta-actions">""") + editButton + deleteButton + Html.Raw("</span>");

        var meta =
            Html.Raw($"""
                <div class="recipe-meta">
                    <span>{Html.Avatar(authorName, authorAvatar).Render()}{Html.Encode(authorName)}</span>
                    <span class="dot">·</span>
                    <span>{Html.Encode(article.PublishedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture))}</span>
                """) +
            metaActions +
            Html.Raw("</div>");

        var cover = article.CoverImageId.Match(
            none: HtmlString.Empty,
            some: imageId => Html.Div("cover", Html.Img($"/images/{imageId.Value}", article.Title.Value, cssClass: "cover-image", style: ctx.FocalStyle($"/images/{imageId.Value}"))));

        var body = breadcrumb +
            Html.H1(article.Title.Value) +
            meta +
            cover +
            Html.Raw($"""<p class="blog-teaser">{Html.Encode(article.Teaser.Value)}</p>""") +
            Html.Div("post-text", Html.Raw(BbcodeRenderer.RenderToHtml(article.Text.Value)));

        return Layout.Page(article.Title.Value, body, ctx, ArticleSeo.Build(article, authorName, baseUrl, ctx.SlugFor(SlugEntityType.Article, article.Id.Value)));
    }

    public static string Form(
        IReadOnlyList<string> errors,
        string title,
        string teaser,
        string text,
        ViewContext ctx,
        string formAction = "/articles",
        string titleKey = "article.new_title",
        Option<ImageId> currentCover = default)
    {
        var (_, t, csrfToken) = ctx;

        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));

        var coverField = currentCover.Match(
            none: () => Html.Label(Html.Text(t("article.field.cover_image")) + Html.InputFile("cover", "image/*")),
            some: imageId =>
                Html.Div("cover", Html.Img($"/images/{imageId.Value}", title, cssClass: "cover-image")) +
                Html.Label(Html.Text(t("article.field.cover_image")) + Html.InputFile("cover", "image/*")) +
                Html.Label(Html.InputCheckbox("remove_cover", "on", false) + Html.Text(t("article.cover_remove"))));

        var formBody =
            Html.CsrfField(csrfToken) +
            Html.Label(Html.Text(t("article.field.title")) + Html.Input("title", title)) +
            Html.Label(Html.Text(t("article.field.teaser")) + Html.Raw($"""<textarea name="teaser" rows="3">{Html.Encode(teaser)}</textarea>""")) +
            Html.Label(Html.Text(t("article.field.text")) + Html.Raw($"""<textarea name="text" rows="10">{Html.Encode(text)}</textarea>""")) +
            coverField +
            Html.Button(t("article.submit"));
        var form = Html.Form(formAction, formBody, enctype: "multipart/form-data");

        var breadcrumb = titleKey == "article.edit_title"
            ? Html.Breadcrumb(
                Crumb.Link(t("blog.title"), "/"),
                Crumb.Link(title, ctx.CanonicalFromAction(SlugEntityType.Article, formAction)),
                Crumb.Current(t("common.edit")))
            : Html.Breadcrumb(
                Crumb.Link(t("blog.title"), "/"),
                Crumb.Current(t("common.new")));

        var body = breadcrumb +
            Html.H1(t(titleKey)) +
            errorHtml +
            form;

        return Layout.Page(t(titleKey), body, ctx);
    }

    // A blog grid card: cover media with date badge, title, excerpt and author. Shared by the
    // home feed and the tag page.
    public static string Card(
        Article a,
        IReadOnlyDictionary<UserId, string> authorNames,
        IReadOnlyDictionary<UserId, Option<ImageId>> authorAvatars,
        ViewContext ctx) =>
        $"""
        <a class="blog-card" href="{ctx.Url(SlugEntityType.Article, a.Id.Value)}">
            {Media(a, ctx)}
            <div class="blog-card-body">
                <h3>{Html.Encode(a.Title.Value)}</h3>
                <p class="blog-excerpt">{Html.Encode(a.Teaser.Value)}</p>
                <div class="blog-foot">{Author(a, authorNames, authorAvatars)}<span>{a.PublishedAt.LocalDateTime:d}</span></div>
            </div>
        </a>
        """;

    private static string DateBadge(Article a)
    {
        var d = a.PublishedAt.LocalDateTime;
        var month = d.ToString("MMM", CultureInfo.InvariantCulture).ToUpperInvariant();
        return $"""<span class="date-badge"><span class="day">{d.Day}</span><span class="month">{Html.Encode(month)}</span></span>""";
    }

    private static string Media(Article a, ViewContext ctx)
    {
        var img = a.CoverImageId.Match(
            none: string.Empty,
            some: id => $"""<img src="/images/{id.Value}" alt="{Html.Encode(a.Title.Value)}"{ctx.FocalStyle($"/images/{id.Value}")} />""");
        return $"""<div class="blog-media">{img}{DateBadge(a)}</div>""";
    }

    private static string Author(
        Article a,
        IReadOnlyDictionary<UserId, string> authorNames,
        IReadOnlyDictionary<UserId, Option<ImageId>> authorAvatars)
    {
        var name = authorNames.GetValueOrNone(a.AuthorId).GetOrElse("?");
        var avatar = authorAvatars.GetValueOrDefault(a.AuthorId, Option<ImageId>.None);
        return $"""<span class="blog-author">{Html.Avatar(name, avatar).Render()}{Html.Encode(name)}</span>""";
    }

    // Home sidebar backed by real data: the newest recipes and the most common recipe tags,
    // each linking to its dedicated tag page (/tag/{slug}).
    private static string Sidebar(IReadOnlyList<Recipe> recipes, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var recent = recipes
            .OrderByDescending(r => r.Id.Value)
            .Take(5)
            .Select((r, i) => $"""<li><a href="{ctx.Url(SlugEntityType.Recipe, r.Id.Value)}"><span class="num">{i + 1:D2}</span><span>{Html.Encode(r.Name.Value)}</span></a></li>""")
            .ToList();

        var recentCard = recent.Count > 0
            ? $"""<section class="sidebar-card"><h4 class="sidebar-head">{Html.Encode(t("blog.sidebar.recent_recipes"))}</h4><ul class="sidebar-recipes">{string.Concat(recent)}</ul></section>"""
            : string.Empty;

        var tags = recipes
            .SelectMany(r => r.Tags)
            .GroupBy(tag => tag.Value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First().Value)
            .Take(14)
            .Select(tag => $"""<a class="sidebar-tag" href="{Html.Encode(ctx.TagUrl(tag))}">{Html.Encode(tag)}</a>""")
            .ToList();

        var tagsCard = tags.Count > 0
            ? $"""<section class="sidebar-card"><h4 class="sidebar-head">{Html.Encode(t("blog.sidebar.popular_tags"))}</h4><div class="sidebar-tags">{string.Concat(tags)}</div></section>"""
            : string.Empty;

        return recentCard + InstagramCard(t) + tagsCard;
    }

    // Two-click embed: the page itself loads nothing from Instagram. Only after the visitor agrees
    // does instagram-consent.js swap the placeholder for Instagram's profile iframe; without JS the
    // plain outbound link remains.
    private static string InstagramCard(Translate t) =>
        $"""
        <section class="sidebar-card">
            <h4 class="sidebar-head">{Html.Encode(t("blog.sidebar.instagram"))}</h4>
            <a class="sidebar-instagram" href="https://www.instagram.com/{InstagramHandle}/" target="_blank" rel="noopener noreferrer">
                {InstagramIcon}
                <span class="sidebar-instagram-text">
                    <span class="sidebar-instagram-handle">@{Html.Encode(InstagramHandle)}</span>
                    <span class="sidebar-instagram-cta">{Html.Encode(t("blog.sidebar.instagram_cta"))}</span>
                </span>
            </a>
            <div class="instagram-embed" data-instagram-embed="https://www.instagram.com/{InstagramHandle}/embed/" data-instagram-title="{Html.Encode(t("blog.sidebar.instagram_frame_title"))}" data-instagram-hide-label="{Html.Encode(t("blog.sidebar.instagram_hide"))}">
                <div class="instagram-consent">
                    <p class="sidebar-instagram-note">{Html.Encode(t("blog.sidebar.instagram_consent"))}</p>
                    <button type="button" class="instagram-load" data-instagram-load>{Html.Encode(t("blog.sidebar.instagram_load"))}</button>
                    <label class="instagram-remember"><input type="checkbox" data-instagram-remember /> {Html.Encode(t("blog.sidebar.instagram_remember"))}</label>
                </div>
            </div>
        </section>
        """;

    private const string InstagramHandle = "the_cooking.couple_";

    private const string InstagramIcon =
        """<svg class="sidebar-instagram-icon" width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="5"/><circle cx="12" cy="12" r="4"/><circle cx="17.5" cy="6.5" r="1" fill="currentColor" stroke="none"/></svg>""";

    private const string PencilIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>""";

    private const string TrashIcon =
        """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9"><path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2m2 0v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/></svg>""";
}
