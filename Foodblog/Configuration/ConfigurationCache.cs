namespace Foodblog.Configuration;

// An in-memory snapshot of the configuration store with typed accessors, mirroring
// TranslationCache: loaded once at startup, refreshed in place after an admin edit so the
// live pipeline (captured in a closure) sees new values without a restart.
public sealed class ConfigurationCache
{
    private readonly IConfigurationRepository _repo;
    private volatile IReadOnlyDictionary<string, string> _values;

    private ConfigurationCache(IReadOnlyDictionary<string, string> values, IConfigurationRepository repo)
    {
        _values = values;
        _repo = repo;
    }

    public static async ValueTask<ConfigurationCache> LoadAsync(IConfigurationRepository repo) =>
        new(await repo.All(), repo);

    public async ValueTask RefreshAsync() => _values = await _repo.All();

    public string Get(string key) => _values.TryGetValue(key, out var value) ? value : string.Empty;

    public string SiteName => Or(ConfigurationKeys.SiteName, "foodblog.ch");

    public string SiteUrl => Get(ConfigurationKeys.SiteUrl);

    // The Instagram account shown on the home page; None (no card, no framing) when unset or invalid.
    public Option<string> InstagramHandle => Configuration.InstagramHandle.ParseOrNone(Get(ConfigurationKeys.InstagramHandle));

    // The year the site went online ("seit 2016"); None when unset or not a year.
    public Option<int> SinceYear => SiteSince.ParseOrNone(Get(ConfigurationKeys.SiteSince));

    // SiteUrl when it is a valid absolute https:// URL; None when unset or invalid.
    public Option<string> PublicBaseUrl => Configuration.SiteUrl.ParseOrNone(SiteUrl);

    public SmtpSettings Smtp => new(
        Host: Get(ConfigurationKeys.SmtpHost),
        Port: int.TryParse(Get(ConfigurationKeys.SmtpPort), out var port) ? port : 587,
        Username: Get(ConfigurationKeys.SmtpUsername),
        Password: Get(ConfigurationKeys.SmtpPassword),
        FromAddress: Get(ConfigurationKeys.SmtpFromAddress),
        FromName: Or(ConfigurationKeys.SmtpFromName, SiteName),
        UseSsl: Get(ConfigurationKeys.SmtpUseSsl) != "false");

    private string Or(string key, string fallback)
    {
        var value = Get(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
