namespace Foodblog.Test;

public sealed class StaticHandlersTests
{
    [Theory]
    [InlineData("hanken-grotesk.woff2")]
    [InlineData("jetbrains-mono.woff2")]
    [InlineData("newsreader.woff2")]
    [InlineData("newsreader-italic.woff2")]
    public async Task Font_serves_the_embedded_woff2(string file)
    {
        var response = await StaticHandlers.Font(file)(Request())(Env());

        Assert.Equal(200, response.Status);
        Assert.Equal("font/woff2", response.ContentType);
        Assert.NotNull(response.Binary);
        Assert.NotEmpty(response.Binary!);
    }

    [Fact]
    public async Task SelectOnClickScript_serves_the_embedded_script()
    {
        var response = await StaticHandlers.SelectOnClickScript(Request())(Env());

        Assert.Equal(200, response.Status);
        Assert.StartsWith("application/javascript", response.ContentType);
        Assert.Contains("data-select-on-click", response.Body);
    }

    [Fact]
    public async Task InstagramConsentScript_serves_the_embedded_script()
    {
        var response = await StaticHandlers.InstagramConsentScript(Request())(Env());

        Assert.Equal(200, response.Status);
        Assert.StartsWith("application/javascript", response.ContentType);
        Assert.Contains("data-instagram-embed", response.Body);
    }

    [Fact]
    public async Task Font_returns_404_for_an_unknown_file()
    {
        var response = await StaticHandlers.Font("../secret.txt")(Request())(Env());

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Asset_serves_the_embedded_banner_png()
    {
        var response = await StaticHandlers.Asset("foodblog-banner.png")(Request())(Env());

        Assert.Equal(200, response.Status);
        Assert.Equal("image/png", response.ContentType);
        Assert.NotNull(response.Binary);
        Assert.NotEmpty(response.Binary!);
    }

    [Fact]
    public async Task Asset_returns_404_for_an_unknown_file()
    {
        var response = await StaticHandlers.Asset("../secret.txt")(Request())(Env());

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task FaviconIco_serves_the_embedded_icon()
    {
        var response = await StaticHandlers.FaviconIco(Request())(Env());

        Assert.Equal(200, response.Status);
        Assert.Equal("image/x-icon", response.ContentType);
        Assert.NotNull(response.Binary);
        Assert.NotEmpty(response.Binary!);
    }

    [Fact]
    public async Task FaviconPng_serves_the_embedded_icon()
    {
        var response = await StaticHandlers.FaviconPng(Request())(Env());

        Assert.Equal(200, response.Status);
        Assert.Equal("image/png", response.ContentType);
        Assert.NotNull(response.Binary);
        Assert.NotEmpty(response.Binary!);
    }

    private static Request Request() =>
        new(HttpMethod.Get, "/fonts/x", Empty, Empty, Empty, Empty);

    private static Env Env() => new(
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
        Pages: new InMemoryPageRepository());

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
