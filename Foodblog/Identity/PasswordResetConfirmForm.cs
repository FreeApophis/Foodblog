namespace Foodblog.Identity;

public static class PasswordResetConfirmForm
{
    public sealed record Valid(string Token, string Password);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var token = request.Form.GetValueOrNone("token").GetOrElse(string.Empty).Trim();
        var password = request.Form.GetValueOrNone("password").GetOrElse(string.Empty);
        var confirmation = request.Form.GetValueOrNone("confirmation").GetOrElse(string.Empty);

        Func<string, string, Valid> create = (t, pwd) => new Valid(t, pwd);

        return create
            .Apply(TryParseToken(token), Combine)
            .Apply(TryParsePassword(password, confirmation), Combine);
    }

    private static Validated<IReadOnlyList<string>, string> TryParseToken(string token) =>
        token.Length > 0
            ? Validated.Succeed<IReadOnlyList<string>, string>(token)
            : Validated.Fail<IReadOnlyList<string>, string>(["auth.error.reset_token_required"]);

    private static Validated<IReadOnlyList<string>, string> TryParsePassword(string password, string confirmation)
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
