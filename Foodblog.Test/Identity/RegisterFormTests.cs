namespace Foodblog.Test.Identity;

public sealed class RegisterFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(Decode("user@blog.de", "Maximilian", "geheim123", "geheim123"));

        Assert.Equal(new Email("user@blog.de"), form.Email);
        Assert.Equal(new DisplayName("Maximilian"), form.DisplayName);
        Assert.Equal("geheim123", form.Password);
    }

    [Fact]
    public void Missing_email_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "Maximilian", "geheim123", "geheim123"));

        Assert.Contains("auth.error.invalid_email", errors);
    }

    [Fact]
    public void Invalid_email_format_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("notanemail", "Maximilian", "geheim123", "geheim123"));

        Assert.Contains("auth.error.invalid_email", errors);
    }

    [Fact]
    public void Short_display_name_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("user@blog.de", "X", "geheim123", "geheim123"));

        Assert.Contains("auth.error.display_name_too_short", errors);
    }

    [Fact]
    public void Short_password_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("user@blog.de", "Maximilian", "kurz", "kurz"));

        Assert.Contains("auth.error.password_too_short", errors);
    }

    [Fact]
    public void Mismatched_passwords_return_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("user@blog.de", "Maximilian", "geheim123", "anders456"));

        Assert.Contains("auth.error.passwords_mismatch", errors);
    }

    [Fact]
    public void All_invalid_fields_accumulate_all_errors()
    {
        var errors = ValidatedAssert.IsFailure(Decode("bad", "X", "kurz", "anders"));

        Assert.Contains("auth.error.invalid_email", errors);
        Assert.Contains("auth.error.display_name_too_short", errors);
        Assert.Contains("auth.error.password_too_short", errors);
        Assert.Contains("auth.error.passwords_mismatch", errors);
    }

    private static Validated<IReadOnlyList<string>, RegisterForm.Valid> Decode(
        string email, string displayName, string password, string confirmation) =>
        RegisterForm.Decode(new Request(
            HttpMethod.Post,
            "/register",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>
            {
                ["email"] = email,
                ["displayName"] = displayName,
                ["password"] = password,
                ["confirmation"] = confirmation,
            },
            new Dictionary<string, string>()));
}
