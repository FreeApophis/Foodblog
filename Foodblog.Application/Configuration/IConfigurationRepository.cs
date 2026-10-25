namespace Foodblog.Application.Configuration;

// A key/value store for runtime-editable site settings (site name, SMTP credentials, base URL).
public interface IConfigurationRepository
{
    ValueTask<IReadOnlyDictionary<string, string>> All();

    ValueTask<Option<string>> Get(string key);

    ValueTask Set(string key, string value);
}
