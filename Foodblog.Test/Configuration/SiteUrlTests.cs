namespace Foodblog.Test.Configuration;

public sealed class SiteUrlTests
{
    [Theory]
    [InlineData("https://foodblog.ch", "https://foodblog.ch")]
    [InlineData("https://foodblog.ch/", "https://foodblog.ch")]
    [InlineData("  https://foodblog.ch/  ", "https://foodblog.ch")]
    [InlineData("https://localhost:65243", "https://localhost:65243")]
    [InlineData("https://example.org/blog/", "https://example.org/blog")]
    public void Accepts_absolute_https_urls_without_a_trailing_slash(string raw, string expected)
    {
        Assert.Equal(Option.Some(expected), SiteUrl.ParseOrNone(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foodblog.ch")]
    [InlineData("/relative")]
    [InlineData("http://foodblog.ch")]
    [InlineData("ftp://foodblog.ch")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://")]
    [InlineData("https://user:pass@foodblog.ch")]
    [InlineData("https://foodblog.ch/?a=b")]
    [InlineData("https://foodblog.ch/#top")]
    public void Rejects_anything_else(string raw)
    {
        FunctionalAssert.None(SiteUrl.ParseOrNone(raw));
    }
}
