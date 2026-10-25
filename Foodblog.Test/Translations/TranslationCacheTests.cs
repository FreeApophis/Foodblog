namespace Foodblog.Test.Translations;

public sealed class TranslationCacheTests
{
    [Fact]
    public async Task Get_returns_text_for_known_key_and_language()
    {
        var repo = await RepoWith("greeting", "de", "Hallo");
        var cache = await TranslationCache.LoadAsync(repo);

        Assert.Equal("Hallo", cache.Get("greeting", "de"));
    }

    [Fact]
    public async Task Get_falls_back_to_standard_language_when_key_not_found_in_requested_language()
    {
        var repo = await RepoWith("greeting", "de", "Hallo");
        var cache = await TranslationCache.LoadAsync(repo);

        Assert.Equal("Hallo", cache.Get("greeting", "en"));
    }

    [Fact]
    public async Task Get_prefers_requested_language_over_fallback()
    {
        var repo = new InMemoryTranslationRepository();
        await repo.Save("greeting", "de", null, "Hallo");
        await repo.Save("greeting", "en", null, "Hello");
        var cache = await TranslationCache.LoadAsync(repo);

        Assert.Equal("Hello", cache.Get("greeting", "en"));
    }

    [Fact]
    public async Task Get_returns_key_itself_when_not_found_in_any_language()
    {
        var cache = await TranslationCache.LoadAsync(new InMemoryTranslationRepository());

        Assert.Equal("missing.key", cache.Get("missing.key", "de"));
    }

    [Fact]
    public async Task GetTranslator_returns_delegate_using_specified_language()
    {
        var repo = new InMemoryTranslationRepository();
        await repo.Save("greeting", "de", null, "Hallo");
        await repo.Save("greeting", "en", null, "Hello");
        var cache = await TranslationCache.LoadAsync(repo);

        var t = cache.GetTranslator("en");

        Assert.Equal("Hello", t("greeting"));
    }

    [Fact]
    public async Task Refresh_updates_translations_from_repository()
    {
        var repo = new InMemoryTranslationRepository();
        var cache = await TranslationCache.LoadAsync(repo);

        await repo.Save("greeting", "de", null, "Hallo");
        await cache.RefreshAsync();

        Assert.Equal("Hallo", cache.Get("greeting", "de"));
    }

    [Fact]
    public async Task Get_auto_registers_missing_key_for_all_supported_languages()
    {
        var repo = new InMemoryTranslationRepository();
        var cache = await TranslationCache.LoadAsync(repo);

        cache.Get("auto.key", "de");

        await cache.FlushPendingAsync();
        var all = await repo.All();
        Assert.Contains(all, t => t.Key == "auto.key" && t.Language == "de");
        Assert.Contains(all, t => t.Key == "auto.key" && t.Language == "en");
    }

    private static async Task<ITranslationRepository> RepoWith(string key, string language, string text)
    {
        var repo = new InMemoryTranslationRepository();
        await repo.Save(key, language, null, text);
        return repo;
    }
}
