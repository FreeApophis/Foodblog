namespace Foodblog.Application.Configuration;

// The default value for every configuration key, written once (per missing key) by the
// startup seeder so a freshly-migrated database always has the full set of keys present.
// SMTP credentials default to blank — they are filled in via the admin settings page.
public static class ConfigurationDefaults
{
    public static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>
    {
        [ConfigurationKeys.SiteName] = "foodblog.ch",
        [ConfigurationKeys.SiteUrl] = string.Empty,
        [ConfigurationKeys.SiteSince] = string.Empty,
        [ConfigurationKeys.InstagramHandle] = string.Empty,
        [ConfigurationKeys.SmtpHost] = string.Empty,
        [ConfigurationKeys.SmtpPort] = "587",
        [ConfigurationKeys.SmtpUsername] = string.Empty,
        [ConfigurationKeys.SmtpPassword] = string.Empty,
        [ConfigurationKeys.SmtpFromAddress] = string.Empty,
        [ConfigurationKeys.SmtpFromName] = string.Empty,
        [ConfigurationKeys.SmtpUseSsl] = "true",
    };
}
