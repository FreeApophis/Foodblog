using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Translations;

public sealed class InMemoryTranslationRepository : ITranslationRepository
{
    private readonly ConcurrentDictionary<(string Key, string Language, string? Variant), Translation> _store = new();

    public ValueTask<IReadOnlyList<Translation>> All() =>
        ValueTask.FromResult<IReadOnlyList<Translation>>(_store.Values.ToList());

    public ValueTask Save(string key, string language, string? variant, string text)
    {
        _store[(key, language, variant)] = new Translation(key, language, variant, text);
        return ValueTask.CompletedTask;
    }
}
