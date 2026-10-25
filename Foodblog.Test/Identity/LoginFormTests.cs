namespace Foodblog.Test.Identity;

public sealed class LoginFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(Decode("user@blog.de", "geheim123"));

        Assert.Equal(new Email("user@blog.de"), form.Email);
        Assert.Equal("geheim123", form.Password);
    }

    [Fact]
    public void Empty_email_returns_failure_with_invalid_email_key()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "geheim123"));

        Assert.Contains("auth.error.invalid_email", errors);
    }

    [Fact]
    public void Invalid_email_format_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("notanemail", "geheim123"));

        Assert.Contains("auth.error.invalid_email", errors);
    }

    [Fact]
    public void Empty_password_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("user@blog.de", string.Empty));

        Assert.Contains("auth.error.password_required", errors);
    }

    [Fact]
    public void Both_missing_accumulates_two_errors()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, string.Empty));

        Assert.Equal(2, errors.Count);
        Assert.Contains("auth.error.invalid_email", errors);
        Assert.Contains("auth.error.password_required", errors);
    }

    private static Validated<IReadOnlyList<string>, LoginForm.Valid> Decode(string email, string password) =>
        LoginForm.Decode(new Request(
            HttpMethod.Post,
            "/login",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string> { ["email"] = email, ["password"] = password },
            new Dictionary<string, string>()));
}
