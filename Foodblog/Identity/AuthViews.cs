namespace Foodblog.Identity;

public static class AuthViews
{
    public static string RegisterForm(IReadOnlyList<string> errors, string email, string displayName, ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var fields =
            Html.Label(Html.Text(t("auth.register.display_name")) + Html.Input("displayName", displayName)) +
            Html.Label(Html.Text(t("auth.register.email")) + Html.InputEmail("email", email)) +
            Html.Label(Html.Text(t("auth.register.password")) + Html.InputPassword("password")) +
            Html.Label(Html.Text(t("auth.register.confirm_password")) + Html.InputPassword("confirmation")) +
            Html.Button(t("auth.register.submit")) +
            Html.P(Html.Link("/login", t("auth.register.already_registered")));
        var body = FormBody(t("auth.register.title"), "/register", errors, fields, ctx);
        return Layout.Page(t("auth.register.title"), body, ctx);
    }

    public static string LoginForm(IReadOnlyList<string> errors, string email, ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var fields =
            Html.Label(Html.Text(t("auth.register.email")) + Html.InputEmail("email", email)) +
            Html.Label(Html.Text(t("auth.register.password")) + Html.InputPassword("password")) +
            Html.Button(t("auth.login.title")) +
            Html.P(Html.Link("/register", t("auth.login.no_account"))) +
            Html.P(Html.Link("/password-reset", t("auth.login.forgot_password")));
        var body = FormBody(t("auth.login.title"), "/login", errors, fields, ctx);
        return Layout.Page(t("auth.login.title"), body, ctx);
    }

    public static string PasswordResetRequestForm(ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var fields =
            Html.Label(Html.Text(t("auth.reset.email")) + Html.InputEmail("email")) +
            Html.Button(t("auth.reset.submit")) +
            Html.P(Html.Link("/login", t("auth.reset.back")));
        var body = FormBody(t("auth.reset.title"), "/password-reset", [], fields, ctx);
        return Layout.Page(t("auth.reset.title"), body, ctx);
    }

    public static string PasswordResetRequested(ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var body = Html.P(Html.Text(t("auth.reset.sent"))) +
            Html.P(Html.Link("/login", t("auth.reset.back")));
        return Layout.Page(t("auth.reset.title"), body, ctx);
    }

    public static string PasswordResetConfirmForm(IReadOnlyList<string> errors, string token, ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var fields =
            Html.InputHidden("token", token) +
            Html.Label(Html.Text(t("auth.reset_confirm.new_password")) + Html.InputPassword("password")) +
            Html.Label(Html.Text(t("auth.reset_confirm.confirm_password")) + Html.InputPassword("confirmation")) +
            Html.Button(t("auth.reset_confirm.submit"));
        var body = FormBody(t("auth.reset_confirm.title"), "/password-reset/confirm", errors, fields, ctx);
        return Layout.Page(t("auth.reset_confirm.page_title"), body, ctx);
    }

    // Shown after registering, and when an unverified user tries to log in: explains that a
    // confirmation link was emailed and offers to resend it.
    public static string VerificationPending(string email, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var resendFields = Html.CsrfField(csrfToken) +
            Html.InputHidden("email", email) +
            Html.Button(t("auth.verify.resend_button"));
        var resend = Html.Form("/verify-email/resend", resendFields);
        var body = Html.H1(t("auth.verify.pending_title")) +
            Html.P(Html.Text(t("auth.verify.pending_message"))) +
            resend +
            Html.P(Html.Link("/login", t("auth.reset.back")));
        return Layout.Page(t("auth.verify.pending_title"), body, ctx);
    }

    // The landing page for the emailed verification link: confirms success or explains an
    // invalid/expired token.
    public static string VerifyResult(bool success, ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var title = success ? t("auth.verify.success_title") : t("auth.verify.invalid_title");
        var message = success ? t("auth.verify.success_message") : t("auth.verify.invalid_message");
        var body = Html.H1(title) +
            Html.P(Html.Text(message)) +
            Html.P(Html.Link("/login", t("nav.login")));
        return Layout.Page(title, body, ctx);
    }

    private static HtmlString FormBody(string title, string action, IReadOnlyList<string> errors, HtmlString fields, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));
        return Html.H1(title) +
            errorHtml +
            Html.Form(action, Html.CsrfField(csrfToken) + fields);
    }
}
