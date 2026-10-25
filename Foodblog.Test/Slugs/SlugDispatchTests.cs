namespace Foodblog.Test.Slugs;

public class SlugDispatchTests
{
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
    private static readonly Request AnyRequest = new(HttpMethod.Get, "/recipes/x", Empty, Empty, Empty, Empty);

    [Fact]
    public async Task Resolve_dispatches_to_the_inner_handler_with_the_owning_id()
    {
        var slugs = new InMemorySlugRepository();
        await slugs.Upsert(SlugEntityType.Recipe, 42, "ruehrkuchen");
        var app = SlugDispatch.Resolve(SlugEntityType.Recipe, "ruehrkuchen", Echo);

        var response = await app(AnyRequest)(BuildEnv(slugs));

        Assert.Equal("42", response.Body);
    }

    [Fact]
    public async Task Resolve_returns_404_for_an_unknown_slug()
    {
        var app = SlugDispatch.Resolve(SlugEntityType.Recipe, "does-not-exist", Echo);

        var response = await app(AnyRequest)(BuildEnv(new InMemorySlugRepository()));

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Resolve_returns_404_when_the_slug_belongs_to_another_entity_type()
    {
        var slugs = new InMemorySlugRepository();
        await slugs.Upsert(SlugEntityType.Article, 42, "ruehrkuchen");
        var app = SlugDispatch.Resolve(SlugEntityType.Recipe, "ruehrkuchen", Echo);

        var response = await app(AnyRequest)(BuildEnv(slugs));

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Resolve_falls_back_to_a_numeric_id_when_no_slug_registry_is_configured()
    {
        var app = SlugDispatch.Resolve(SlugEntityType.Recipe, "7", Echo);

        var response = await app(AnyRequest)(BuildEnv(slugs: null));

        Assert.Equal("7", response.Body);
    }

    [Fact]
    public async Task Resolve_returns_404_for_a_non_numeric_segment_when_no_slug_registry_is_configured()
    {
        var app = SlugDispatch.Resolve(SlugEntityType.Recipe, "ruehrkuchen", Echo);

        var response = await app(AnyRequest)(BuildEnv(slugs: null));

        Assert.Equal(404, response.Status);
    }

    private static Func<int, App> Echo => id => _ => _ => ValueTask.FromResult(Response.Text(id.ToString()));

    private static Env BuildEnv(ISlugRepository? slugs) => new(
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
        Slugs: slugs);
}
