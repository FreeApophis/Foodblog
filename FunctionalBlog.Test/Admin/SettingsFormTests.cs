namespace FunctionalBlog.Test.Admin;

public sealed class SettingsFormTests
{
    [Fact]
    public void Decodes_a_complete_valid_form()
    {
        var form = ValidatedAssert.IsSuccess(SettingsForm.Decode(Request(
            siteName: "Mein Blog",
            siteUrl: "https://blog.example.com",
            host: "mail.example.com",
            port: "465",
            username: "user",
            password: "secret",
            fromAddress: "no-reply@example.com",
            fromName: "Blog",
            useSsl: true)));

        Assert.Equal("Mein Blog", form.SiteName);
        Assert.Equal("https://blog.example.com", form.SiteUrl);
        Assert.Equal("mail.example.com", form.SmtpHost);
        Assert.Equal(465, form.SmtpPort);
        Assert.Equal("secret", form.SmtpPassword);
        Assert.Equal("no-reply@example.com", form.SmtpFromAddress);
        Assert.True(form.SmtpUseSsl);
    }

    [Fact]
    public void An_empty_site_name_fails()
    {
        var errors = ValidatedAssert.IsFailure(SettingsForm.Decode(Request(siteName: string.Empty)));

        Assert.Contains("settings.error.site_name_required", errors);
    }

    [Fact]
    public void A_non_numeric_or_out_of_range_port_fails()
    {
        Assert.Contains("settings.error.port_invalid", ValidatedAssert.IsFailure(SettingsForm.Decode(Request(port: "abc"))));
        Assert.Contains("settings.error.port_invalid", ValidatedAssert.IsFailure(SettingsForm.Decode(Request(port: "99999"))));
    }

    [Fact]
    public void An_invalid_sender_address_fails_but_a_blank_one_is_allowed()
    {
        Assert.Contains("settings.error.from_address_invalid", ValidatedAssert.IsFailure(SettingsForm.Decode(Request(fromAddress: "notanemail"))));
        ValidatedAssert.IsSuccess(SettingsForm.Decode(Request(fromAddress: string.Empty)));
    }

    [Theory]
    [InlineData("foodblog.ch")]
    [InlineData("http://foodblog.ch")]
    [InlineData("/relative")]
    public void A_site_url_that_is_not_absolute_https_fails(string siteUrl)
    {
        Assert.Contains("settings.error.site_url_invalid", ValidatedAssert.IsFailure(SettingsForm.Decode(Request(siteUrl: siteUrl))));
    }

    [Fact]
    public void A_site_url_is_stored_without_a_trailing_slash()
    {
        var form = ValidatedAssert.IsSuccess(SettingsForm.Decode(Request(siteUrl: "https://foodblog.ch/")));

        Assert.Equal("https://foodblog.ch", form.SiteUrl);
    }

    [Fact]
    public void A_blank_site_url_is_allowed()
    {
        var form = ValidatedAssert.IsSuccess(SettingsForm.Decode(Request(siteUrl: string.Empty)));

        Assert.Equal(string.Empty, form.SiteUrl);
    }

    [Fact]
    public void An_unchecked_ssl_box_decodes_to_false()
    {
        var form = ValidatedAssert.IsSuccess(SettingsForm.Decode(Request(useSsl: false)));

        Assert.False(form.SmtpUseSsl);
    }

    [Fact]
    public void Errors_accumulate_across_fields()
    {
        var errors = ValidatedAssert.IsFailure(SettingsForm.Decode(Request(siteName: string.Empty, port: "abc", fromAddress: "bad")));

        Assert.Contains("settings.error.site_name_required", errors);
        Assert.Contains("settings.error.port_invalid", errors);
        Assert.Contains("settings.error.from_address_invalid", errors);
    }

    private static Request Request(
        string siteName = "Blog",
        string siteUrl = "",
        string host = "",
        string port = "587",
        string username = "",
        string password = "",
        string fromAddress = "",
        string fromName = "",
        bool useSsl = true)
    {
        var form = new Dictionary<string, string>
        {
            ["site_name"] = siteName,
            ["site_url"] = siteUrl,
            ["smtp_host"] = host,
            ["smtp_port"] = port,
            ["smtp_username"] = username,
            ["smtp_password"] = password,
            ["smtp_from_address"] = fromAddress,
            ["smtp_from_name"] = fromName,
        };

        if (useSsl)
        {
            form["smtp_use_ssl"] = "true";
        }

        return new FunctionalBlog.Request(HttpMethod.Post, "/admin/settings", Empty, Empty, form, Empty);
    }

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
