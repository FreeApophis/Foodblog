namespace Foodblog.Images;

public static class ImageCleanupViews
{
    // `deleted` is true after a bulk removal, so the empty state can confirm the cleanup rather
    // than imply there was never anything to remove.
    public static string Cleanup(IReadOnlyList<ImageSummary> orphans, ViewContext ctx, bool deleted = false)
    {
        var (_, t, csrfToken) = ctx;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("image.cleanup.title")));

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("admin.dashboard.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(t("image.cleanup.title"))}</h1></div>
                <p class="page-head-blurb">{Html.Encode(t("image.cleanup.blurb"))}</p>
            </div>
            """);

        HtmlString content;
        if (orphans.Count == 0)
        {
            content = Html.P(Html.Text(t(deleted ? "image.cleanup.removed" : "image.cleanup.none")));
        }
        else
        {
            var deleteForm = Html.Form(
                "/admin/images/cleanup/delete",
                Html.CsrfField(csrfToken) + Html.Button($"{t("image.cleanup.delete_all")} ({orphans.Count})"),
                confirm: t("image.cleanup.confirm"));

            var gallery = Html.Div("image-gallery", HtmlString.Concat(orphans.Select(Card)));

            content = Html.P(Html.Text(t("image.cleanup.found"))) + deleteForm + gallery;
        }

        return Layout.Page(t("image.cleanup.title"), breadcrumb + head + content, ctx);
    }

    private static HtmlString Card(ImageSummary image) =>
        Html.Div("image-card", Html.Img($"/images/{image.Id.Value}", image.FileName) + Html.Small(image.FileName));
}
