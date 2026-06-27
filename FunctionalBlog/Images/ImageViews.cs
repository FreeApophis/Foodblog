using System.Globalization;

namespace FunctionalBlog.Images;

public static class ImageViews
{
    public static string Library(IReadOnlyList<ImageSummary> images, ViewContext ctx, IReadOnlyList<string>? errors = null)
    {
        var (_, t, csrfToken) = ctx;

        var errorHtml = errors is { Count: > 0 }
            ? Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))))
            : HtmlString.Empty;

        var uploadBody = Html.CsrfField(csrfToken) +
            Html.Label(Html.Text(t("image.field.file")) + Html.InputFile("file", "image/*")) +
            Html.Button(t("image.upload"));
        var uploadForm = Html.Form("/images", uploadBody, enctype: "multipart/form-data");

        var gallery = images.Count == 0
            ? Html.P(Html.Text(t("image.empty")))
            : Html.Div("image-gallery", HtmlString.Concat(images.Select(image => Card(image, ctx))));

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("image.library_title")));

        var body = breadcrumb +
            Html.H1(t("image.library_title")) +
            errorHtml +
            uploadForm +
            gallery;

        return Layout.Page(t("image.library_title"), body, ctx);
    }

    private static HtmlString Card(ImageSummary image, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var url = $"/images/{image.Id.Value}";

        var deleteForm = Html.Form(
            $"/images/{image.Id.Value}/delete",
            Html.CsrfField(csrfToken) + Html.Button(t("common.delete")),
            style: "display:inline",
            confirm: t("common.confirm_delete"));

        var snippet = $"[img]{url}[/img]";
        var embedField = Html.Raw($"""<input class="embed-snippet" type="text" value="{Html.Encode(snippet)}" readonly onclick="this.select()" />""");

        var cardBody = FocalEditor(image, url, ctx, csrfToken) +
            Html.Small(image.FileName) +
            embedField +
            deleteForm;

        return Html.Div("image-card", cardBody);
    }

    // Click-to-set crop focal point. The picture sits inside a [data-focal] box that focal-point.js
    // turns clicks into focal_x/focal_y fractions over, then submits this form to persist them. The
    // marker dot shows the current focal point; the thumbnail mirrors the live object-position.
    private static HtmlString FocalEditor(ImageSummary image, string url, ViewContext ctx, string csrfToken)
    {
        var dot = $"""<span class="focal-dot" style="left:{Percent(image.Focal.X)};top:{Percent(image.Focal.Y)}"></span>""";
        var pick = $"""<div class="focal-pick" data-focal title="{Html.Encode(ctx.T("image.focal.hint"))}"><img src="{Html.Encode(url)}" alt="{Html.Encode(image.FileName)}"{ctx.FocalStyle(url)} />{dot}</div>""";

        var body = Html.CsrfField(csrfToken) +
            Html.Raw(pick) +
            Html.InputHidden("focal_x", image.Focal.X.ToString("0.####", CultureInfo.InvariantCulture)) +
            Html.InputHidden("focal_y", image.Focal.Y.ToString("0.####", CultureInfo.InvariantCulture));

        return Html.Form($"/images/{image.Id.Value}/focal", body, cssClass: "focal-form");
    }

    private static string Percent(double fraction) =>
        (fraction * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
}
