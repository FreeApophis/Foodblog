namespace Foodblog.Identity;

public static class ChangePasswordForm
{
    public sealed record Valid(string CurrentPassword, string NewPassword);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var current = request.Form.GetValueOrNone("current").GetOrElse(string.Empty);
        var password = request.Form.GetValueOrNone("password").GetOrElse(string.Empty);
        var confirmation = request.Form.GetValueOrNone("confirmation").GetOrElse(string.Empty);

        Func<string, string, Valid> create = (cur, pwd) => new Valid(cur, pwd);

        return create
            .Apply(TryParseCurrentPassword(current), Combine)
            .Apply(TryParseNewPassword(password, confirmation), Combine);
    }

    private static Validated<IReadOnlyList<string>, string> TryParseCurrentPassword(string current) =>
        current.Length > 0
            ? Validated.Succeed<IReadOnlyList<string>, string>(current)
            : Validated.Fail<IReadOnlyList<string>, string>(["auth.error.current_password_required"]);

    private static Validated<IReadOnlyList<string>, string> TryParseNewPassword(string password, string confirmation)
    {
        Func<bool, bool, string> alwaysPassword = (_, _) => password;

        return alwaysPassword
            .Apply(CheckPasswordLength(password), Combine)
            .Apply(CheckPasswordMatch(password, confirmation), Combine);
    }

    private static Validated<IReadOnlyList<string>, bool> CheckPasswordLength(string password) =>
        password.Length >= 8
            ? Validated.Succeed<IReadOnlyList<string>, bool>(true)
            : Validated.Fail<IReadOnlyList<string>, bool>(["auth.error.password_too_short"]);

    private static Validated<IReadOnlyList<string>, bool> CheckPasswordMatch(string password, string confirmation) =>
        password == confirmation
            ? Validated.Succeed<IReadOnlyList<string>, bool>(true)
            : Validated.Fail<IReadOnlyList<string>, bool>(["auth.error.passwords_mismatch"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
