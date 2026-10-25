namespace Foodblog.Test.Articles;

public sealed class BlogHandlerTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02];

    [Fact]
    public async Task CreateArticle_with_a_cover_image_saves_the_image_and_links_it()
    {
        var env = BuildEnv();
        var request = AFormRequest() with { Files = [new UploadedFile("cover", "titel.png", "image/png", PngBytes)] };

        var response = await BlogHandlers.CreateArticle(request)(env);

        Assert.Equal(303, response.Status);
        var article = Assert.Single(await env.Articles.All());
        var coverId = FunctionalAssert.Some(article.CoverImageId);
        FunctionalAssert.Some(await env.Images.Find(coverId));
    }

    [Fact]
    public async Task CreateArticle_without_a_cover_image_leaves_it_unset()
    {
        var env = BuildEnv();

        var response = await BlogHandlers.CreateArticle(AFormRequest())(env);

        Assert.Equal(303, response.Status);
        var article = Assert.Single(await env.Articles.All());
        FunctionalAssert.None(article.CoverImageId);
        Assert.Empty(await env.Images.List());
    }

    [Fact]
    public async Task CreateArticle_rejects_an_invalid_cover_image()
    {
        var env = BuildEnv();
        var request = AFormRequest() with { Files = [new UploadedFile("cover", "schad.exe", "image/png", [0x4D, 0x5A, 0x90])] };

        var response = await BlogHandlers.CreateArticle(request)(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Articles.All());
        Assert.Empty(await env.Images.List());
    }

    [Fact]
    public async Task Index_renders_a_featured_post_and_a_card_grid_for_the_rest()
    {
        var env = BuildEnv();
        var coverId = await env.Images.NextId();
        await env.Images.Save(Image.Create(coverId, "titel.png", ImageContentType.Png, PngBytes, new UserId(1), env.Clock.Now));

        for (var i = 0; i < 4; i++)
        {
            var id = await env.Articles.NextId();
            await env.Articles.Save(Article.Create(
                id,
                new ArticleTitle($"Artikel {i}"),
                new ArticleTeaser("Ein ausreichend langer Teaser."),
                new ArticleText("Ein ausreichend langer Text."),
                new UserId(1),
                env.Clock.Now.AddMinutes(-i),
                i == 0 ? Option.Some(coverId) : Option<ImageId>.None));
        }

        var response = await BlogHandlers.Index(AnEmptyRequest())(env);

        Assert.Contains($"/images/{coverId.Value}", response.Body);
        Assert.Contains("blog-featured", response.Body);
        Assert.Contains("blog-grid", response.Body);
        Assert.Contains("blog-card", response.Body);
    }

    [Fact]
    public async Task Index_links_the_instagram_profile_without_loading_anything_from_instagram()
    {
        var env = BuildEnv();

        var response = await BlogHandlers.Index(AnEmptyRequest())(env);

        Assert.Contains("""href="https://www.instagram.com/the_cooking.couple_/" """, response.Body);
        Assert.Contains("rel=\"noopener noreferrer\"", response.Body);
        Assert.DoesNotContain("<iframe", response.Body);
        Assert.DoesNotContain("""src="https://www.instagram.com""", response.Body);
    }

    [Fact]
    public async Task Index_offers_to_load_the_instagram_embed_only_after_consent()
    {
        var env = BuildEnv();

        var response = await BlogHandlers.Index(AnEmptyRequest())(env);

        Assert.Contains("""data-instagram-embed="https://www.instagram.com/the_cooking.couple_/embed/" """, response.Body);
        Assert.Contains("data-instagram-load", response.Body);
        Assert.Contains("data-instagram-remember", response.Body);
        Assert.DoesNotContain("<iframe", response.Body);
    }

    [Fact]
    public async Task ShowArticle_renders_the_cover_image_when_present()
    {
        var env = BuildEnv();
        var imageId = await env.Images.NextId();
        await env.Images.Save(Image.Create(imageId, "titel.png", ImageContentType.Png, PngBytes, new UserId(1), env.Clock.Now));
        var articleId = await env.Articles.NextId();
        await env.Articles.Save(Article.Create(
            articleId,
            new ArticleTitle("Mit Bild"),
            new ArticleTeaser("Ein ausreichend langer Teaser."),
            new ArticleText("Ein ausreichend langer Text."),
            new UserId(1),
            env.Clock.Now,
            Option.Some(imageId)));

        var response = await BlogHandlers.ShowArticle(articleId)(AnEmptyRequest())(env);

        Assert.Contains($"/images/{imageId.Value}", response.Body);
    }

    [Fact]
    public async Task ShowArticle_emits_blogposting_json_ld_and_open_graph_tags()
    {
        var env = BuildEnv();
        var imageId = await env.Images.NextId();
        await env.Images.Save(Image.Create(imageId, "titel.png", ImageContentType.Png, PngBytes, new UserId(1), env.Clock.Now));
        var articleId = await env.Articles.NextId();
        await env.Articles.Save(Article.Create(
            articleId,
            new ArticleTitle("Mit Bild"),
            new ArticleTeaser("Ein ausreichend langer Teaser."),
            new ArticleText("Ein ausreichend langer Text."),
            new UserId(1),
            env.Clock.Now,
            Option.Some(imageId)));

        var request = AnEmptyRequest() with { BaseUrl = "https://foodblog.ch" };
        var response = await BlogHandlers.ShowArticle(articleId)(request)(env);

        Assert.Contains("\"@type\":\"BlogPosting\"", response.Body);
        Assert.Contains($"https://foodblog.ch/articles/{articleId.Value}", response.Body);
        Assert.Contains($"<meta property=\"og:image\" content=\"https://foodblog.ch/images/{imageId.Value}\" />", response.Body);
    }

    private static Request AFormRequest()
    {
        var form = new Dictionary<string, string>
        {
            ["title"] = "Mein Titel",
            ["teaser"] = "Ein ausreichend langer Teaser.",
            ["text"] = "Ein ausreichend langer Text.",
        };
        return new Request(HttpMethod.Post, "/articles", Empty, Empty, form, Empty);
    }

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/", Empty, Empty, Empty, Empty);

    private static AuthenticatedUser AuthUser()
    {
        var user = User.Create(
            new UserId(1),
            new Email("admin@blog.de"),
            new DisplayName("Admin"),
            "hash",
            [],
            DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, []);
    }

    private static Env BuildEnv() => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: AuthUser(),
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository());

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
