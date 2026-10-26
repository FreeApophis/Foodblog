namespace Foodblog.Application.Configuration;

// Canonical keys for the configuration store. Centralized so handlers, the cache and the
// seeder all refer to the same strings.
public static class ConfigurationKeys
{
    public const string SiteName = "site.name";
    public const string SiteUrl = "site.url";
    public const string SiteSince = "site.since";
    public const string InstagramHandle = "social.instagram";
    public const string SmtpHost = "smtp.host";
    public const string SmtpPort = "smtp.port";
    public const string SmtpUsername = "smtp.username";
    public const string SmtpPassword = "smtp.password";
    public const string SmtpFromAddress = "smtp.from_address";
    public const string SmtpFromName = "smtp.from_name";
    public const string SmtpUseSsl = "smtp.use_ssl";
}
