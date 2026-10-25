using Foodblog.Test.Search;

namespace Foodblog.Test.Admin;

public sealed class AdminSearchHandlerTests
{
    [Fact]
    public async Task Status_returns_200_and_lists_document_counts()
    {
        var search = new FakeSearchIndex
        {
            StatusValue = new SearchIndexStatus(Option.Some(DateTimeOffset.UtcNow), 2, 1, 5, 3),
        };
        var env = BuildEnv(search);

        var response = await AdminSearchHandlers.Status(ARequest(HttpMethod.Get, "/admin/search"))(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("admin.search.field.articles", response.Body);
        Assert.Contains("admin.search.field.total", response.Body);
        Assert.Contains("admin.search.rebuild", response.Body);

        // The rebuild button must give in-flight feedback (it can take many seconds): it disables
        // itself and shows a busy/spinner state while the request runs.
        Assert.Contains("hx-disabled-elt", response.Body);
        Assert.Contains("admin.search.rebuilding", response.Body);
        Assert.False(search.Rebuilt);
    }

    [Fact]
    public async Task Rebuild_rebuilds_the_index_and_confirms()
    {
        var search = new FakeSearchIndex();
        var env = BuildEnv(search);

        var response = await AdminSearchHandlers.Rebuild(ARequest(HttpMethod.Post, "/admin/search/rebuild"))(env);

        Assert.Equal(200, response.Status);
        Assert.True(search.Rebuilt);
        Assert.Contains("admin.search.rebuilt", response.Body);
    }

    private static Env BuildEnv(ISearchIndex search) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Search: search);

    private static Request ARequest(HttpMethod method, string path) =>
        new(method, path, Empty, Empty, Empty, Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
