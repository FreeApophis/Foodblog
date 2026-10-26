namespace Foodblog.Test.Configuration;

public sealed class ConfigurationCacheTests
{
    [Fact]
    public async Task SiteName_falls_back_to_the_brand_when_unset()
    {
        var cache = await ConfigurationCache.LoadAsync(new InMemoryConfigurationRepository());

        Assert.Equal("foodblog.ch", cache.SiteName);
    }

    [Fact]
    public async Task Reads_site_name_and_typed_smtp_settings()
    {
        var repo = new InMemoryConfigurationRepository();
        await repo.Set(ConfigurationKeys.SiteName, "Mein Blog");
        await repo.Set(ConfigurationKeys.SmtpHost, "mail.example.com");
        await repo.Set(ConfigurationKeys.SmtpPort, "465");
        await repo.Set(ConfigurationKeys.SmtpFromAddress, "no-reply@example.com");
        await repo.Set(ConfigurationKeys.SmtpUseSsl, "true");

        var cache = await ConfigurationCache.LoadAsync(repo);

        Assert.Equal("Mein Blog", cache.SiteName);
        Assert.Equal("mail.example.com", cache.Smtp.Host);
        Assert.Equal(465, cache.Smtp.Port);
        Assert.Equal("no-reply@example.com", cache.Smtp.FromAddress);
        Assert.True(cache.Smtp.UseSsl);
        Assert.True(cache.Smtp.IsConfigured);

        // The sender name falls back to the site name when unset.
        Assert.Equal("Mein Blog", cache.Smtp.FromName);
    }

    [Fact]
    public async Task Smtp_is_not_configured_when_host_is_blank()
    {
        var cache = await ConfigurationCache.LoadAsync(new InMemoryConfigurationRepository());

        Assert.False(cache.Smtp.IsConfigured);
        Assert.Equal(587, cache.Smtp.Port);
    }

    [Fact]
    public async Task RefreshAsync_picks_up_changes()
    {
        var repo = new InMemoryConfigurationRepository();
        await repo.Set(ConfigurationKeys.SiteName, "Alt");
        var cache = await ConfigurationCache.LoadAsync(repo);
        Assert.Equal("Alt", cache.SiteName);

        await repo.Set(ConfigurationKeys.SiteName, "Neu");
        await cache.RefreshAsync();

        Assert.Equal("Neu", cache.SiteName);
    }

    [Fact]
    public async Task InstagramHandle_is_none_when_unset()
    {
        var cache = await ConfigurationCache.LoadAsync(new InMemoryConfigurationRepository());

        FunctionalAssert.None(cache.InstagramHandle);
    }

    [Fact]
    public void InstagramHandle_is_the_stored_handle()
    {
        var cache = TestConfiguration.With((ConfigurationKeys.InstagramHandle, "the_cooking.couple_"));

        Assert.Equal(Option.Some("the_cooking.couple_"), cache.InstagramHandle);
    }

    [Fact]
    public void InstagramHandle_is_none_when_the_stored_value_is_not_a_handle()
    {
        var cache = TestConfiguration.With((ConfigurationKeys.InstagramHandle, "\"><script>"));

        FunctionalAssert.None(cache.InstagramHandle);
    }

    [Fact]
    public async Task SinceYear_is_none_when_unset()
    {
        var cache = await ConfigurationCache.LoadAsync(new InMemoryConfigurationRepository());

        FunctionalAssert.None(cache.SinceYear);
    }

    [Theory]
    [InlineData("2016", 2016)]
    [InlineData(" 1999 ", 1999)]
    public void SinceYear_is_the_stored_year(string stored, int expected)
    {
        var cache = TestConfiguration.With((ConfigurationKeys.SiteSince, stored));

        Assert.Equal(Option.Some(expected), cache.SinceYear);
    }

    [Fact]
    public void SinceYear_is_none_when_the_stored_value_is_not_a_year()
    {
        var cache = TestConfiguration.With((ConfigurationKeys.SiteSince, "seit immer"));

        FunctionalAssert.None(cache.SinceYear);
    }
}
