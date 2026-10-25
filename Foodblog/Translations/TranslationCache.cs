using System.Collections.Concurrent;

namespace Foodblog.Translations;

public sealed class TranslationCache
{
    private readonly ITranslationRepository _repo;
    private readonly ConcurrentDictionary<string, bool> _registering = new();
    private volatile IReadOnlyDictionary<(string Key, string Language, string? Variant), string> _dict;
    private volatile Task _pendingRegistration = Task.CompletedTask;

    private TranslationCache(IReadOnlyDictionary<(string Key, string Language, string? Variant), string> dict, ITranslationRepository repo)
    {
        _dict = dict;
        _repo = repo;
    }

    public static async ValueTask<TranslationCache> LoadAsync(ITranslationRepository repo)
    {
        var all = await repo.All();
        return new TranslationCache(Build(all), repo);
    }

    public Translate GetTranslator(string language, string? variant = null) =>
        key => Get(key, language, variant);

    public string Get(string key, string language, string? variant = null)
    {
        if (variant != null && _dict.TryGetValue((key, language, variant), out var t1))
        {
            return t1;
        }

        if (_dict.TryGetValue((key, language, null), out var t2))
        {
            return t2;
        }

        if (_dict.TryGetValue((key, Languages.Default, null), out var t3))
        {
            return t3;
        }

        AutoRegister(key);
        return key;
    }

    public async ValueTask RefreshAsync()
    {
        var all = await _repo.All();
        _dict = Build(all);
    }

    public Task FlushPendingAsync() => _pendingRegistration;

    private void AutoRegister(string key)
    {
        if (!_registering.TryAdd(key, true))
        {
            return;
        }

        _pendingRegistration = Task.Run(async () =>
        {
            foreach (var lang in Languages.Supported)
            {
                if (!_dict.ContainsKey((key, lang, null)))
                {
                    await _repo.Save(key, lang, null, key);
                }
            }

            await RefreshAsync();
        });
    }

    private static IReadOnlyDictionary<(string Key, string Language, string? Variant), string> Build(
        IReadOnlyList<Translation> all) =>
        all.ToDictionary(t => (t.Key, t.Language, t.Variant), t => t.Text);
}
