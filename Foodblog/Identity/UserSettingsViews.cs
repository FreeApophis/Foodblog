namespace Foodblog.Identity;

public static class UserSettingsViews
{
    public static string Settings(AuthenticatedUser user, IReadOnlyList<string> passwordErrors, IReadOnlyList<string> avatarErrors, ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var body = Html.H1(t("settings.title")) +
            Html.P(Html.Text($"{t("settings.logged_in_as")}: {user.DisplayName.Value} ({user.Email.Value})")) +
            AvatarSection(user, avatarErrors, ctx) +
            ChangePasswordSection(passwordErrors, ctx);
        return Layout.Page(t("settings.title"), body, ctx);
    }

    private static HtmlString AvatarSection(AuthenticatedUser user, IReadOnlyList<string> errors, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));

        var preview = Html.Div("avatar-settings", Html.Avatar(user.DisplayName.Value, user.AvatarImageId, "avatar-lg"));

        var uploadBody = Html.CsrfField(csrfToken) +
            Html.Label(Html.Text(t("settings.avatar.choose")) + Html.InputFile("avatar", "image/*")) +
            Html.Button(t("settings.avatar.submit"));
        var uploadForm = Html.Form("/settings/avatar", uploadBody, enctype: "multipart/form-data");

        var removeBody = Html.CsrfField(csrfToken) + Html.InputHidden("remove_avatar", "on") + Html.Button(t("settings.avatar.remove"));
        var removeForm = user.AvatarImageId is [_]
            ? Html.Form("/settings/avatar", removeBody, style: "display:inline", confirm: t("common.confirm_delete"))
            : HtmlString.Empty;

        return Html.H2(Html.Text(t("settings.avatar.title"))) + errorHtml + preview + uploadForm + removeForm;
    }

    private static HtmlString ChangePasswordSection(IReadOnlyList<string> errors, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));
        var formBody =
            Html.CsrfField(csrfToken) +
            Html.Label(Html.Text(t("settings.change_password.current")) + Html.InputPassword("current")) +
            Html.Label(Html.Text(t("settings.change_password.new")) + Html.InputPassword("password")) +
            Html.Label(Html.Text(t("settings.change_password.confirm")) + Html.InputPassword("confirmation")) +
            Html.Button(t("settings.change_password.submit"));
        return Html.H2(Html.Text(t("settings.change_password.title"))) +
            errorHtml +
            Html.Form("/settings/password", formBody);
    }
}
