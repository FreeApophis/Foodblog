using System.Net;

namespace FunctionalBlog;

public static class Html
{
    public static HtmlString Text(string value) => new HtmlString.Encoded(value);

    public static HtmlString Raw(string html) => new HtmlString.Safe(html);

    public static string Encode(string value) => WebUtility.HtmlEncode(value);

    public static HtmlString H1(string text) => new HtmlString.Safe($"<h1>{Encode(text)}</h1>");

    public static HtmlString H2(HtmlString body) => new HtmlString.Safe($"<h2>{body.Render()}</h2>");

    public static HtmlString P(HtmlString body) => new HtmlString.Safe($"<p>{body.Render()}</p>");

    public static HtmlString Small(string text) => new HtmlString.Safe($"<small>{Encode(text)}</small>");

    public static HtmlString Article(HtmlString body) => new HtmlString.Safe($"<article>{body.Render()}</article>");

    public static HtmlString Article(string cssClass, HtmlString body) =>
        new HtmlString.Safe($"<article class=\"{Encode(cssClass)}\">{body.Render()}</article>");

    public static HtmlString LinkBlock(string href, string cssClass, HtmlString body) =>
        new HtmlString.Safe($"""<a href="{Encode(href)}" class="{Encode(cssClass)}">{body.Render()}</a>""");

    public static HtmlString Div(string cssClass, HtmlString body) => new HtmlString.Safe($"<div class=\"{Encode(cssClass)}\">{body.Render()}</div>");

    public static HtmlString Link(string href, string text) => new HtmlString.Safe($"<a href=\"{Encode(href)}\">{Encode(text)}</a>");

    // A page-navigation bar: previous/next plus a numbered link per page, each pointing at
    // {basePath}?page=N. Renders nothing when everything fits on a single page.
    public static HtmlString Pagination(int currentPage, int totalPages, string basePath, string ariaLabel)
    {
        if (totalPages <= 1)
        {
            return HtmlString.Empty;
        }

        string PageLink(int page, string label, string? rel = null)
        {
            var relAttr = rel is null ? string.Empty : $" rel=\"{rel}\"";
            return $"""<a class="page-link" href="{Encode(basePath)}?page={page}"{relAttr}>{Encode(label)}</a>""";
        }

        string Disabled(string label) =>
            $"""<span class="page-link is-disabled" aria-hidden="true">{Encode(label)}</span>""";

        var prev = currentPage > 1 ? PageLink(currentPage - 1, "‹", "prev") : Disabled("‹");
        var next = currentPage < totalPages ? PageLink(currentPage + 1, "›", "next") : Disabled("›");

        var numbers = string.Concat(Enumerable.Range(1, totalPages).Select(page =>
            page == currentPage
                ? $"""<span class="page-link is-current" aria-current="page">{page}</span>"""
                : PageLink(page, page.ToString())));

        return new HtmlString.Safe($"""<nav class="pagination" aria-label="{Encode(ariaLabel)}">{prev}{numbers}{next}</nav>""");
    }

    // A breadcrumb trail rendered as the design's mono eyebrow row: muted links joined by "/",
    // with the current (href-less) crumb accented. Replaces the old per-page "Back" links.
    public static HtmlString Breadcrumb(params Crumb[] crumbs)
    {
        string Render(Crumb c) => c.Href is { } href
            ? $"""<a href="{Encode(href)}">{Encode(c.Label)}</a>"""
            : $"""<span class="crumb-current" aria-current="page">{Encode(c.Label)}</span>""";

        var trail = string.Join(
            """<span class="crumb-sep" aria-hidden="true">/</span>""",
            crumbs.Select(Render));

        return new HtmlString.Safe($"""<nav class="breadcrumb" aria-label="Breadcrumb">{trail}</nav>""");
    }

    // A round avatar: the user's uploaded image when one is set, otherwise the coloured circle
    // with their initial. `extraClass` lets callers size it (e.g. "avatar-lg" for a preview).
    public static HtmlString Avatar(string name, Option<ImageId> avatarId, string extraClass = "")
    {
        var cls = string.IsNullOrEmpty(extraClass) ? "avatar" : $"avatar {extraClass}";
        var initial = name.Length > 0 ? name[..1].ToUpperInvariant() : "?";

        return avatarId is [var id]
            ? new HtmlString.Safe($"""<span class="{Encode(cls)} avatar-img"><img src="/images/{id.Value}" alt="{Encode(name)}" /></span>""")
            : new HtmlString.Safe($"""<span class="{Encode(cls)}">{Encode(initial)}</span>""");
    }

    public static HtmlString Button(string text) => new HtmlString.Safe($"<button type=\"submit\">{Encode(text)}</button>");

    public static HtmlString Form(string action, HtmlString body, Option<string> cssClass = default, Option<string> style = default, Option<string> enctype = default, Option<string> confirm = default)
    {
        var classAttr = cssClass.Match(none: string.Empty, some: c => $" class=\"{Encode(c)}\"");
        var styleAttr = style.Match(none: string.Empty, some: s => $" style=\"{Encode(s)}\"");
        var enctypeAttr = enctype.Match(none: string.Empty, some: e => $" enctype=\"{Encode(e)}\"");
        var confirmAttr = confirm.Match(none: string.Empty, some: c => $" data-confirm=\"{Encode(c)}\"");

        return new HtmlString.Safe($"""<form method="post" action="{Encode(action)}"{classAttr}{styleAttr}{enctypeAttr}{confirmAttr}>{body.Render()}</form>""");
    }

    public static HtmlString InputFile(string name, string accept, bool multiple = false) =>
        new HtmlString.Safe($"""<input type="file" name="{Encode(name)}" accept="{Encode(accept)}"{(multiple ? " multiple" : string.Empty)} />""");

    // `style` is a pre-built, already-safe attribute fragment (with a leading space), e.g. the
    // ` style="object-position:.."` produced by ViewContext.FocalStyle; pass "" for none.
    public static HtmlString Img(string src, string alt, Option<string> cssClass = default, string style = "")
    {
        var classAttr = cssClass.Match(none: string.Empty, some: c => $" class=\"{Encode(c)}\"");

        return new HtmlString.Safe($"""<img src="{Encode(src)}" alt="{Encode(alt)}"{classAttr}{style} />""");
    }

    public static HtmlString Fieldset(string legend, HtmlString body) =>
        new HtmlString.Safe($"""<fieldset><legend>{Encode(legend)}</legend>{body.Render()}</fieldset>""");

    public static HtmlString Label(HtmlString body) => new HtmlString.Safe($"<label>{body.Render()}</label>");

    public static HtmlString Input(string name, string value = "", Option<string> style = default)
    {
        var styleAttr = style.Match(none: string.Empty, some: s => $" style=\"{Encode(s)}\"");

        return new HtmlString.Safe($"""<input name="{Encode(name)}" value="{Encode(value)}"{styleAttr} />""");
    }

    public static HtmlString InputNumber(string name, string value, string min = "0", Option<string> step = default, bool autofocus = false)
    {
        var stepAttr = step.Match(none: string.Empty, some: s => $" step=\"{Encode(s)}\"");
        var autofocusAttr = autofocus ? " autofocus" : string.Empty;

        return new HtmlString.Safe($"""<input name="{Encode(name)}" type="number"{stepAttr} min="{Encode(min)}" value="{Encode(value)}"{autofocusAttr} />""");
    }

    public static HtmlString InputEmail(string name, string value = "") =>
        new HtmlString.Safe($"""<input type="email" name="{Encode(name)}" value="{Encode(value)}" />""");

    public static HtmlString InputPassword(string name) =>
        new HtmlString.Safe($"""<input type="password" name="{Encode(name)}" />""");

    public static HtmlString InputHidden(string name, string value) =>
        new HtmlString.Safe($"""<input type="hidden" name="{Encode(name)}" value="{Encode(value)}" />""");

    public static HtmlString CsrfField(string token) => InputHidden("_csrf", token);

    public static HtmlString InputCheckbox(string name, string value, bool isChecked) =>
        new HtmlString.Safe($"""<input type="checkbox" name="{Encode(name)}" value="{Encode(value)}"{(isChecked ? " checked" : string.Empty)} />""");

    public static HtmlString Table(HtmlString body) => new HtmlString.Safe($"<table>{body.Render()}</table>");

    public static HtmlString Thead(HtmlString body) => new HtmlString.Safe($"<thead>{body.Render()}</thead>");

    public static HtmlString Tbody(HtmlString body) => new HtmlString.Safe($"<tbody>{body.Render()}</tbody>");

    public static HtmlString Tr(HtmlString body) => new HtmlString.Safe($"<tr>{body.Render()}</tr>");

    public static HtmlString Th(string text) => new HtmlString.Safe($"<th>{Encode(text)}</th>");

    public static HtmlString Td(HtmlString body, int colspan = 1) =>
        new HtmlString.Safe(colspan > 1
            ? $"""<td colspan="{colspan}">{body.Render()}</td>"""
            : $"<td>{body.Render()}</td>");

    public static HtmlString Ul(IEnumerable<HtmlString> items) =>
        new HtmlString.Safe("<ul>" + string.Join(string.Empty, items.Select(x => $"<li>{x.Render()}</li>")) + "</ul>");

    public static HtmlString Ol(IEnumerable<HtmlString> items) =>
        new HtmlString.Safe("<ol>" + string.Join(string.Empty, items.Select(x => $"<li>{x.Render()}</li>")) + "</ol>");

    public static HtmlString Paragraphs(string text) =>
        new HtmlString.Safe(string.Join(string.Empty, text
            .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => P(Text(x)).Render())));
}
