namespace Foodblog.Pages;

public static class PageViews
{
    public static string Index(IReadOnlyList<Page> pages, ViewContext ctx)
    {
        var (principal, t, _) = ctx;

        var items = pages.Count == 0
            ? Html.P(Html.Text(t("page.empty")))
            : Html.Ul(pages.Select(p => Html.Link(ctx.Url(SlugEntityType.Page, p.Id.Value), p.Title.Value)));

        var body = Html.H1(t("page.list_title")) +
            (principal.Can<Create>(new PageResource())
                ? Html.P(Html.Link("/pages/new", t("page.new_page")))
                : HtmlString.Empty) +
            items;

        return Layout.Page(t("page.list_title"), body, ctx);
    }

    public static string Show(Page page, ViewContext ctx, string baseUrl)
    {
        var (principal, t, csrfToken) = ctx;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("page.list_title"), "/pages"),
            Crumb.Current(page.Title.Value));

        var actions = new List<HtmlString>();
        if (principal.Can<Edit>(new PageResource()))
        {
            actions.Add(Html.Link($"/pages/{page.Id.Value}/edit", t("common.edit")));
        }

        if (principal.Can<Delete>(new PageResource()))
        {
            actions.Add(Html.Form($"/pages/{page.Id.Value}/delete", Html.CsrfField(csrfToken) + Html.Button(t("common.delete")), style: "display:inline", confirm: t("common.confirm_delete")));
        }

        var actionsBar = actions.Count > 0 ? Html.P(HtmlString.Join(" · ", actions)) : HtmlString.Empty;

        var body = breadcrumb +
            actionsBar +
            Html.H1(page.Title.Value) +
            Html.Div("post-text", Html.Raw(BbcodeRenderer.RenderToHtml(page.Content.Value)));

        return Layout.Page(page.Title.Value, body, ctx, PageSeo.Build(page, baseUrl, ctx.SlugFor(SlugEntityType.Page, page.Id.Value)));
    }

    public static string Form(
        IReadOnlyList<string> errors,
        string title,
        string content,
        ViewContext ctx,
        string formAction = "/pages",
        string titleKey = "page.new_title")
    {
        var (_, t, csrfToken) = ctx;

        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));

        var formBody =
            Html.CsrfField(csrfToken) +
            Html.Label(Html.Text(t("page.field.title")) + Html.Input("title", title)) +
            Html.Label(Html.Text(t("page.field.content")) + Html.Raw($"""<textarea name="content" rows="16">{Html.Encode(content)}</textarea>""")) +
            Html.P(Html.Small(t("markup.hint"))) +
            Html.Button(t("page.submit"));
        var form = Html.Form(formAction, formBody);

        var breadcrumb = titleKey == "page.edit_title"
            ? Html.Breadcrumb(
                Crumb.Link(t("page.list_title"), "/pages"),
                Crumb.Link(title, ctx.CanonicalFromAction(SlugEntityType.Page, formAction)),
                Crumb.Current(t("common.edit")))
            : Html.Breadcrumb(
                Crumb.Link(t("page.list_title"), "/pages"),
                Crumb.Current(t("common.new")));

        var body = breadcrumb +
            Html.H1(t(titleKey)) +
            errorHtml +
            form;

        return Layout.Page(t(titleKey), body, ctx);
    }
}
