namespace Foodblog.Test.Pages;

public sealed class PageHandlerTests
{
    [Fact]
    public async Task CreatePage_saves_the_page_and_redirects_to_it()
    {
        var env = BuildEnv();
        var request = AFormRequest(("title", "Impressum"), ("content", "Angaben gemäß Gesetz."));

        var response = await PageHandlers.CreatePage(request)(env);

        Assert.Equal(303, response.Status);
        var page = Assert.Single(await env.Pages.All());
        Assert.Equal("Impressum", page.Title.Value);
        Assert.Equal($"/pages/{page.Id.Value}", response.Headers["Location"]);
    }

    [Fact]
    public async Task CreatePage_rejects_a_too_short_title()
    {
        var env = BuildEnv();
        var request = AFormRequest(("title", "AB"), ("content", "Inhalt."));

        var response = await PageHandlers.CreatePage(request)(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Pages.All());
    }

    [Fact]
    public async Task CreatePage_rejects_empty_content()
    {
        var env = BuildEnv();
        var request = AFormRequest(("title", "Über uns"), ("content", "   "));

        var response = await PageHandlers.CreatePage(request)(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Pages.All());
    }

    [Fact]
    public async Task ShowPage_renders_bbcode_content_including_a_gallery_image()
    {
        var env = BuildEnv();
        var id = await env.Pages.NextId();
        await env.Pages.Save(Page.Create(id, new PageTitle("Galerie"), new PageContent("Ein [b]Bild[/b]: [img]/images/9[/img]")));

        var response = await PageHandlers.ShowPage(id)(AnEmptyRequest())(env);

        Assert.Contains("<strong>Bild</strong>", response.Body);
        Assert.Contains("""<img src="/images/9" """, response.Body);
    }

    [Fact]
    public async Task ShowPage_emits_open_graph_tags_for_sharing()
    {
        var env = BuildEnv();
        var id = await env.Pages.NextId();
        await env.Pages.Save(Page.Create(id, new PageTitle("Impressum"), new PageContent("Verantwortlich: [b]Anna[/b].")));

        var request = AnEmptyRequest() with { BaseUrl = "https://foodblog.ch" };
        var response = await PageHandlers.ShowPage(id)(request)(env);

        Assert.Contains($"<meta property=\"og:url\" content=\"https://foodblog.ch/pages/{id.Value}\" />", response.Body);
        Assert.Contains("<meta property=\"og:description\" content=\"Verantwortlich: Anna.\" />", response.Body);
    }

    [Fact]
    public async Task ShowPage_returns_404_for_an_unknown_page()
    {
        var env = BuildEnv();

        var response = await PageHandlers.ShowPage(new PageId(987_654))(AnEmptyRequest())(env);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task UpdatePage_overwrites_title_and_content()
    {
        var env = BuildEnv();
        var id = await env.Pages.NextId();
        await env.Pages.Save(Page.Create(id, new PageTitle("Alt"), new PageContent("Alt.")));
        var request = AFormRequest(("title", "Neuer Titel"), ("content", "Neuer Inhalt."));

        var response = await PageHandlers.UpdatePage(id)(request)(env);

        Assert.Equal(303, response.Status);
        var page = FunctionalAssert.Some(await env.Pages.Find(id));
        Assert.Equal("Neuer Titel", page.Title.Value);
        Assert.Equal("Neuer Inhalt.", page.Content.Value);
    }

    [Fact]
    public async Task DeletePage_removes_the_page()
    {
        var env = BuildEnv();
        var id = await env.Pages.NextId();
        await env.Pages.Save(Page.Create(id, new PageTitle("Weg"), new PageContent("Inhalt.")));

        var response = await PageHandlers.DeletePage(id)(AnEmptyRequest())(env);

        Assert.Equal(303, response.Status);
        FunctionalAssert.None(await env.Pages.Find(id));
    }

    private static Request AFormRequest(params (string Key, string Value)[] fields)
    {
        var form = fields.ToDictionary(f => f.Key, f => f.Value);
        return new Request(HttpMethod.Post, "/pages", Empty, Empty, form, Empty);
    }

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/", Empty, Empty, Empty, Empty);

    private static Env BuildEnv() => new(
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
