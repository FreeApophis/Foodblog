namespace Foodblog.Test.Identity;

public sealed class EmailTests
{
    [Fact]
    public void Parse_returns_email_with_normalized_value_for_valid_address()
    {
        Assert.Equal(Option.Some(new Email("user@example.com")), Email.ParseOrNone("User@Example.COM"));
    }

    [Fact]
    public void Parse_trims_surrounding_whitespace()
    {
        Assert.Equal(Option.Some(new Email("test@example.com")), Email.ParseOrNone("  test@example.com  "));
    }

    [Fact]
    public void Parse_returns_none_for_missing_at_sign()
    {
        FunctionalAssert.None(Email.ParseOrNone("notanemail"));
    }

    [Fact]
    public void Parse_returns_none_for_empty_local_part()
    {
        FunctionalAssert.None(Email.ParseOrNone("@example.com"));
    }

    [Fact]
    public void Parse_returns_none_for_missing_dot_in_domain()
    {
        FunctionalAssert.None(Email.ParseOrNone("user@example"));
    }

    [Fact]
    public void Parse_returns_none_for_empty_string()
    {
        FunctionalAssert.None(Email.ParseOrNone(string.Empty));
    }
}
