namespace Foodblog.Test.Admin;

public sealed class AdminSettingsHandlersTests
{
    [Fact]
    public async Task Show_returns_the_settings_page()
    {
        var config = new InMemoryConfigurationRepository();
        await config.Set(ConfigurationKeys.SiteName, "Test Blog");
        var env = await BuildEnv(config);

        var response = await AdminSettingsHandlers.Show(GetRequest())(env);

        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task Save_persists_values_and_refreshes_the_cache()
    {
        var config = new InMemoryConfigurationRepository();
        var env = await BuildEnv(config);

        var response = await AdminSettingsHandlers.Save(SaveRequest(siteName: "Neuer Name", host: "mail.x.com", port: "2525"))(env);

        Assert.Equal(200, response.Status);
        Assert.Equal(Option.Some("Neuer Name"), await config.Get(ConfigurationKeys.SiteName));
        Assert.Equal(Option.Some("mail.x.com"), await config.Get(ConfigurationKeys.SmtpHost));
        Assert.Equal(Option.Some("2525"), await config.Get(ConfigurationKeys.SmtpPort));
        Assert.Equal("Neuer Name", env.Config!.SiteName);
    }

    [Fact]
    public async Task Save_persists_the_since_year_and_instagram_handle()
    {
        var config = new InMemoryConfigurationRepository();
        var env = await BuildEnv(config);

        await AdminSettingsHandlers.Save(SaveRequest(since: "2016", instagram: "@the_cooking.couple_"))(env);

        Assert.Equal(Option.Some("2016"), await config.Get(ConfigurationKeys.SiteSince));
        Assert.Equal(Option.Some("the_cooking.couple_"), await config.Get(ConfigurationKeys.InstagramHandle));
        Assert.Equal(Option.Some("the_cooking.couple_"), env.Config!.InstagramHandle);
    }

    [Fact]
    public async Task Show_renders_the_since_year_and_instagram_fields()
    {
        var config = new InMemoryConfigurationRepository();
        await config.Set(ConfigurationKeys.InstagramHandle, "the_cooking.couple_");
        var env = await BuildEnv(config);

        var response = await AdminSettingsHandlers.Show(GetRequest())(env);

        Assert.Contains("name=\"site_since\"", response.Body);
        Assert.Contains("name=\"instagram_handle\"", response.Body);
        Assert.Contains("value=\"the_cooking.couple_\"", response.Body);
    }

    [Fact]
    public async Task Save_with_an_invalid_form_returns_400_and_persists_nothing()
    {
        var config = new InMemoryConfigurationRepository();
        var env = await BuildEnv(config);

        var response = await AdminSettingsHandlers.Save(SaveRequest(siteName: string.Empty))(env);

        Assert.Equal(400, response.Status);
        FunctionalAssert.None(await config.Get(ConfigurationKeys.SiteName));
    }

    [Fact]
    public async Task Save_with_a_blank_password_keeps_the_stored_one()
    {
        var config = new InMemoryConfigurationRepository();
        await config.Set(ConfigurationKeys.SmtpPassword, "geheim");
        var env = await BuildEnv(config);

        await AdminSettingsHandlers.Save(SaveRequest(password: string.Empty))(env);

        Assert.Equal(Option.Some("geheim"), await config.Get(ConfigurationKeys.SmtpPassword));
    }

    [Fact]
    public async Task Save_with_a_new_password_overwrites_it()
    {
        var config = new InMemoryConfigurationRepository();
        await config.Set(ConfigurationKeys.SmtpPassword, "alt");
        var env = await BuildEnv(config);

        await AdminSettingsHandlers.Save(SaveRequest(password: "neu"))(env);

        Assert.Equal(Option.Some("neu"), await config.Get(ConfigurationKeys.SmtpPassword));
    }

    [Fact]
    public async Task TestEmail_sends_to_the_given_recipient()
    {
        var email = new RecordingEmailSender();
        var env = await BuildEnv(new InMemoryConfigurationRepository(), email);

        var response = await AdminSettingsHandlers.TestEmail(TestEmailRequest("admin@blog.de"))(env);

        Assert.Equal(200, response.Status);
        Assert.Single(email.Sent);
        Assert.Equal("admin@blog.de", email.Sent[0].To);
    }

    [Fact]
    public async Task TestEmail_with_an_invalid_recipient_returns_400()
    {
        var env = await BuildEnv(new InMemoryConfigurationRepository(), new RecordingEmailSender());

        var response = await AdminSettingsHandlers.TestEmail(TestEmailRequest("notanemail"))(env);

        Assert.Equal(400, response.Status);
    }

    private static async Task<Env> BuildEnv(IConfigurationRepository config, IEmailSender? email = null) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Configuration: config,
        Config: await ConfigurationCache.LoadAsync(config),
        Email: email);

    private static Request GetRequest() =>
        new(HttpMethod.Get, "/admin/settings", Empty, Empty, Empty, Empty);

    private static Request SaveRequest(
        string siteName = "Blog",
        string host = "",
        string port = "587",
        string password = "",
        string since = "",
        string instagram = "")
    {
        var form = new Dictionary<string, string>
        {
            ["site_name"] = siteName,
            ["site_since"] = since,
            ["instagram_handle"] = instagram,
            ["site_url"] = string.Empty,
            ["smtp_host"] = host,
            ["smtp_port"] = port,
            ["smtp_username"] = string.Empty,
            ["smtp_password"] = password,
            ["smtp_from_address"] = string.Empty,
            ["smtp_from_name"] = string.Empty,
            ["smtp_use_ssl"] = "true",
        };
        return new Request(HttpMethod.Post, "/admin/settings", Empty, Empty, form, Empty);
    }

    private static Request TestEmailRequest(string to) =>
        new(
            HttpMethod.Post,
            "/admin/settings/test-email",
            Empty,
            Empty,
            new Dictionary<string, string> { ["test_email"] = to },
            Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
