namespace Foodblog.Test.Configuration;

public sealed class InstagramHandleTests
{
    [Theory]
    [InlineData("the_cooking.couple_", "the_cooking.couple_")]
    [InlineData("@the_cooking.couple_", "the_cooking.couple_")]
    [InlineData("  koch123  ", "koch123")]
    public void Accepts_a_plain_handle_without_the_at_sign(string raw, string expected)
    {
        Assert.Equal(Option.Some(expected), InstagramHandle.ParseOrNone(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("@")]
    [InlineData("zwei worte")]
    [InlineData("a/b")]
    [InlineData("\"><script>")]
    [InlineData("https://www.instagram.com/koch")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    public void Rejects_anything_that_is_not_a_handle(string raw)
    {
        FunctionalAssert.None(InstagramHandle.ParseOrNone(raw));
    }
}
