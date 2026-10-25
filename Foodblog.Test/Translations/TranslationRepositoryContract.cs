namespace Foodblog.Test.Translations;

public abstract class TranslationRepositoryContract
{
    [Fact]
    public async Task All_returns_empty_for_fresh_repository()
    {
        var repo = CreateRepository();

        Assert.Empty(await repo.All());
    }

    [Fact]
    public async Task Save_then_All_contains_saved_translation()
    {
        var repo = CreateRepository();

        await repo.Save("blog.title", "de", null, "Blog");

        var all = await repo.All();
        Assert.Contains(all, t => t.Key == "blog.title" && t.Language == "de" && t.Text == "Blog");
    }

    [Fact]
    public async Task Save_preserves_variant()
    {
        var repo = CreateRepository();

        await repo.Save("blog.title", "de", "at", "Blog (AT)");

        var all = await repo.All();
        Assert.Contains(all, t => t.Key == "blog.title" && t.Language == "de" && t.Variant == "at");
    }

    [Fact]
    public async Task Save_overwrites_existing_translation_with_same_key_language_and_variant()
    {
        var repo = CreateRepository();
        await repo.Save("blog.title", "de", null, "Blog");

        await repo.Save("blog.title", "de", null, "Aktualisiert");

        var all = await repo.All();
        var match = Assert.Single(all, t => t.Key == "blog.title" && t.Language == "de" && t.Variant == null);
        Assert.Equal("Aktualisiert", match.Text);
    }

    [Fact]
    public async Task Save_null_and_non_null_variant_are_different_entries()
    {
        var repo = CreateRepository();

        await repo.Save("blog.title", "de", null, "Standard");
        await repo.Save("blog.title", "de", "at", "Österreich");

        var all = await repo.All();
        Assert.Equal(2, all.Count(t => t.Key == "blog.title" && t.Language == "de"));
    }

    protected abstract ITranslationRepository CreateRepository();
}
