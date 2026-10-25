using System.Collections.Concurrent;
using Foodblog.Application.Configuration;

namespace Foodblog.DataAccess.Configuration;

public sealed class InMemoryConfigurationRepository : IConfigurationRepository
{
    private readonly ConcurrentDictionary<string, string> _values = new();

    public ValueTask<IReadOnlyDictionary<string, string>> All() =>
        ValueTask.FromResult<IReadOnlyDictionary<string, string>>(
            _values.ToDictionary(kv => kv.Key, kv => kv.Value));

    public ValueTask<Option<string>> Get(string key) =>
        ValueTask.FromResult(_values.GetValueOrNone(key));

    public ValueTask Set(string key, string value)
    {
        _values[key] = value;
        return ValueTask.CompletedTask;
    }
}
