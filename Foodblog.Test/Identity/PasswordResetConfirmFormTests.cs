namespace Foodblog.Test.Identity;

public sealed class PasswordResetConfirmFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(Decode("valid-token", "neues-passwort1", "neues-passwort1"));

        Assert.Equal("valid-token", form.Token);
        Assert.Equal("neues-passwort1", form.Password);
    }

    [Fact]
    public void Empty_token_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "neues-passwort1", "neues-passwort1"));

        Assert.Contains("auth.error.reset_token_required", errors);
    }

    [Fact]
    public void Short_password_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("valid-token", "kurz", "kurz"));

        Assert.Contains("auth.error.password_too_short", errors);
    }

    [Fact]
    public void Mismatched_passwords_return_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("valid-token", "neues-passwort1", "anders-passwort"));

        Assert.Contains("auth.error.passwords_mismatch", errors);
    }

    [Fact]
    public void All_three_invalid_accumulates_three_errors()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "kurz", "anders"));

        Assert.Contains("auth.error.reset_token_required", errors);
        Assert.Contains("auth.error.password_too_short", errors);
        Assert.Contains("auth.error.passwords_mismatch", errors);
    }

    private static Validated<IReadOnlyList<string>, PasswordResetConfirmForm.Valid> Decode(
        string token, string password, string confirmation) =>
        PasswordResetConfirmForm.Decode(new Request(
            HttpMethod.Post,
            "/password-reset/confirm",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>
            {
                ["token"] = token,
                ["password"] = password,
                ["confirmation"] = confirmation,
            },
            new Dictionary<string, string>()));
}
