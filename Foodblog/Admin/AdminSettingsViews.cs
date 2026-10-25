namespace Foodblog.Admin;

public static class AdminSettingsViews
{
    // The /admin/settings page: the site-name + SMTP form and a "send test email" panel.
    public static string Page(IReadOnlyDictionary<string, string> values, IReadOnlyList<string> errors, bool saved, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        string V(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("admin.settings.title")));

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("admin.dashboard.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(t("admin.settings.title"))}</h1></div>
                <p class="page-head-blurb">{Html.Encode(t("admin.settings.blurb"))}</p>
            </div>
            """);

        var savedNote = saved
            ? Html.Div("eyebrow eyebrow-accent", Html.Text(t("admin.settings.saved")))
            : HtmlString.Empty;

        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));

        var siteFields =
            Html.Label(Html.Text(t("admin.settings.site_name")) + Html.Input("site_name", V(ConfigurationKeys.SiteName))) +
            Html.Label(Html.Text(t("admin.settings.site_url")) + Html.Input("site_url", V(ConfigurationKeys.SiteUrl)));

        var smtpFields =
            Html.Label(Html.Text(t("admin.settings.smtp_host")) + Html.Input("smtp_host", V(ConfigurationKeys.SmtpHost))) +
            Html.Label(Html.Text(t("admin.settings.smtp_port")) + Html.InputNumber("smtp_port", V(ConfigurationKeys.SmtpPort), min: "1")) +
            Html.Label(Html.Text(t("admin.settings.smtp_username")) + Html.Input("smtp_username", V(ConfigurationKeys.SmtpUsername))) +
            Html.Label(Html.Text(t("admin.settings.smtp_password")) + Html.InputPassword("smtp_password")) +
            Html.P(Html.Small(t("admin.settings.smtp_password_hint"))) +
            Html.Label(Html.Text(t("admin.settings.smtp_from_address")) + Html.InputEmail("smtp_from_address", V(ConfigurationKeys.SmtpFromAddress))) +
            Html.Label(Html.Text(t("admin.settings.smtp_from_name")) + Html.Input("smtp_from_name", V(ConfigurationKeys.SmtpFromName))) +
            Html.Label(Html.InputCheckbox("smtp_use_ssl", "true", V(ConfigurationKeys.SmtpUseSsl) != "false") + Html.Text(t("admin.settings.smtp_use_ssl")));

        var formBody = Html.CsrfField(csrfToken) +
            Html.Fieldset(t("admin.settings.section_site"), siteFields) +
            Html.Fieldset(t("admin.settings.section_smtp"), smtpFields) +
            Html.Button(t("admin.settings.save"));

        var body = breadcrumb + head + savedNote + errorHtml +
            Html.Form("/admin/settings", formBody) +
            TestPanel(ctx);

        return Layout.Page(t("admin.settings.title"), body, ctx);
    }

    // The inner HTML swapped into #settings-test-result after a test send.
    public static string TestResult(bool ok, string message, ViewContext ctx) =>
        Html.Div(ok ? "eyebrow eyebrow-accent" : "errors", Html.Text(message)).Render();

    private static HtmlString TestPanel(ViewContext ctx)
    {
        var (principal, t, csrfToken) = ctx;
        var defaultTo = principal is AuthenticatedUser user ? user.Email.Value : string.Empty;

        var inner = Html.CsrfField(csrfToken) +
            Html.Label(Html.Text(t("admin.settings.test_to")) + Html.InputEmail("test_email", defaultTo)) +
            Html.Raw($"""<button type="submit" hx-post="/admin/settings/test-email" hx-target="#settings-test-result" hx-swap="innerHTML">{Html.Encode(t("admin.settings.test_send"))}</button>""");

        return Html.Raw($"""<section class="card"><h2>{Html.Encode(t("admin.settings.test_title"))}</h2><p class="muted">{Html.Encode(t("admin.settings.test_blurb"))}</p>""") +
            Html.Form("/admin/settings/test-email", inner) +
            Html.Raw("""<div id="settings-test-result"></div></section>""");
    }
}
