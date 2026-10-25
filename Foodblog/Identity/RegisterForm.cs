namespace Foodblog.Identity;

public static class RegisterForm
{
    public sealed record Valid(Email Email, DisplayName DisplayName, string Password);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var emailRaw = request.Form.GetValueOrNone("email").GetOrElse(string.Empty).Trim();
        var displayNameRaw = request.Form.GetValueOrNone("displayName").GetOrElse(string.Empty).Trim();
        var password = request.Form.GetValueOrNone("password").GetOrElse(string.Empty);
        var confirmation = request.Form.GetValueOrNone("confirmation").GetOrElse(string.Empty);

        Func<Email, DisplayName, string, Valid> create = (email, displayName, password) => new Valid(email, displayName, password);

        return create
            .Apply(TryParseEmail(emailRaw), Combine)
            .Apply(TryParseDisplayName(displayNameRaw), Combine)
            .Apply(TryParsePassword(password, confirmation), Combine);
    }

    private static Validated<IReadOnlyList<string>, Email> TryParseEmail(string raw) =>
        Email.ParseOrNone(raw).Match(
            none: () => Validated.Fail<IReadOnlyList<string>, Email>(["auth.error.invalid_email"]),
            some: Validated.Succeed<IReadOnlyList<string>, Email>);

    private static Validated<IReadOnlyList<string>, DisplayName> TryParseDisplayName(string raw) =>
        raw.Length >= 2
            ? Validated.Succeed<IReadOnlyList<string>, DisplayName>(new DisplayName(raw))
            : Validated.Fail<IReadOnlyList<string>, DisplayName>(["auth.error.display_name_too_short"]);

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
