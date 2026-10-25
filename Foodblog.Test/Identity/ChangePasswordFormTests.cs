namespace Foodblog.Test.Identity;

public sealed class ChangePasswordFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(Decode("altes-passwort", "neues-passwort1", "neues-passwort1"));

        Assert.Equal("altes-passwort", form.CurrentPassword);
        Assert.Equal("neues-passwort1", form.NewPassword);
    }

    [Fact]
    public void Empty_current_password_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "neues-passwort1", "neues-passwort1"));

        Assert.Contains("auth.error.current_password_required", errors);
    }

    [Fact]
    public void Short_new_password_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("altes-passwort", "kurz", "kurz"));

        Assert.Contains("auth.error.password_too_short", errors);
    }

    [Fact]
    public void Mismatched_passwords_return_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("altes-passwort", "neues-passwort1", "anders-passwort"));

        Assert.Contains("auth.error.passwords_mismatch", errors);
    }

    [Fact]
    public void All_three_invalid_accumulates_three_errors()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "kurz", "anders"));

        Assert.Contains("auth.error.current_password_required", errors);
        Assert.Contains("auth.error.password_too_short", errors);
        Assert.Contains("auth.error.passwords_mismatch", errors);
    }

    private static Validated<IReadOnlyList<string>, ChangePasswordForm.Valid> Decode(
        string current, string password, string confirmation) =>
        ChangePasswordForm.Decode(new Request(
            HttpMethod.Post,
            "/settings",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>
            {
                ["current"] = current,
                ["password"] = password,
                ["confirmation"] = confirmation,
            },
            new Dictionary<string, string>()));
}
