namespace Foodblog.Identity;

public static class LoginForm
{
    public sealed record Valid(Email Email, string Password);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var emailRaw = request.Form.GetValueOrNone("email").GetOrElse(string.Empty).Trim();
        var password = request.Form.GetValueOrNone("password").GetOrElse(string.Empty);

        Func<Email, string, Valid> create = (email, pwd) => new Valid(email, pwd);

        return create
            .Apply(TryParseEmail(emailRaw), Combine)
            .Apply(TryParsePassword(password), Combine);
    }

    private static Validated<IReadOnlyList<string>, Email> TryParseEmail(string raw) =>
        Email.ParseOrNone(raw).Match(
            none: () => Validated.Fail<IReadOnlyList<string>, Email>(["auth.error.invalid_email"]),
            some: Validated.Succeed<IReadOnlyList<string>, Email>);

    private static Validated<IReadOnlyList<string>, string> TryParsePassword(string password) =>
        password.Length > 0
            ? Validated.Succeed<IReadOnlyList<string>, string>(password)
            : Validated.Fail<IReadOnlyList<string>, string>(["auth.error.password_required"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
