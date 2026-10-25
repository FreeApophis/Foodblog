namespace Foodblog.Test;

public sealed class TranslationSeederTests
{
    [Fact]
    public async Task Seeds_common_back_to_admin()
    {
        var repo = await SeedAsync();
        Assert.Contains(await repo.All(), t => t.Key == "common.back_to_admin" && t.Language == "de");
    }

    [Fact]
    public async Task Seeds_admin_users_detail_title()
    {
        var repo = await SeedAsync();
        Assert.Contains(await repo.All(), t => t.Key == "admin.users.detail_title" && t.Language == "de");
    }

    [Fact]
    public async Task Seeds_admin_roles_detail_title()
    {
        var repo = await SeedAsync();
        Assert.Contains(await repo.All(), t => t.Key == "admin.roles.detail_title" && t.Language == "de");
    }

    [Fact]
    public async Task Seeds_search_type_labels()
    {
        var repo = await SeedAsync();
        var all = await repo.All();
        Assert.Contains(all, t => t.Key == "search.type.article" && t.Language == "de");
        Assert.Contains(all, t => t.Key == "search.type.recipe" && t.Language == "de");
        Assert.Contains(all, t => t.Key == "search.type.ingredient" && t.Language == "de");
    }

    [Fact]
    public async Task Seeds_error_forbidden_strings()
    {
        var repo = await SeedAsync();
        var all = await repo.All();
        Assert.Contains(all, t => t.Key == "error.forbidden_heading" && t.Language == "de");
        Assert.Contains(all, t => t.Key == "error.forbidden_message" && t.Language == "de");
    }

    [Fact]
    public async Task Seeds_error_notfound_strings()
    {
        var repo = await SeedAsync();
        var all = await repo.All();
        Assert.Contains(all, t => t.Key == "error.notfound_heading" && t.Language == "de");
        Assert.Contains(all, t => t.Key == "error.notfound_message" && t.Language == "de");
    }

    private static async Task<InMemoryTranslationRepository> SeedAsync()
    {
        var repo = new InMemoryTranslationRepository();
        await TranslationSeeder.SeedAsync(repo);
        return repo;
    }
}
