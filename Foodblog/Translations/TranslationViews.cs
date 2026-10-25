namespace Foodblog.Translations;

public static class TranslationViews
{
    public static string List(IReadOnlyList<Translation> translations, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var grouped = translations
            .GroupBy(tr => tr.Key)
            .OrderBy(g => g.Key)
            .ToList();

        var langHeaders = HtmlString.Concat(Languages.Supported.Select(lang => Html.Th(lang.ToUpperInvariant())));

        HtmlString Row(IGrouping<string, Translation> group)
        {
            var cells = HtmlString.Concat(Languages.Supported.Select(lang =>
            {
                var entry = group.FirstOrDefault(tr => tr.Language == lang && tr.Variant == null);
                var text = entry?.Text ?? string.Empty;
                var form = Html.Form(
                    $"/admin/translations/{Uri.EscapeDataString(group.Key)}/{lang}",
                    Html.CsrfField(csrfToken) + Html.Input("text", text, style: "width:100%") + Html.Button(t("translations.save")));
                return Html.Td(form);
            }));
            return Html.Tr(Html.Td(Html.Raw($"<code>{Html.Encode(group.Key)}</code>")) + cells);
        }

        var rows = grouped.Count == 0
            ? Html.Tr(Html.Td(Html.Raw("–"), colspan: 5))
            : HtmlString.Concat(grouped.Select(Row));

        var table = Html.Table(
            Html.Thead(Html.Tr(Html.Th(t("translations.key")) + langHeaders)) +
            Html.Tbody(rows));

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("translations.title")));

        var body = breadcrumb +
            Html.H1(t("translations.title")) +
            Html.P(Html.Link("/admin/translations/export.json", t("translations.export"))) +
            table;

        return Layout.Page(t("translations.title"), body, ctx);
    }
}
