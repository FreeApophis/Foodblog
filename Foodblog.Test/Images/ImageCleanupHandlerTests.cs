namespace Foodblog.Test.Images;

public sealed class ImageCleanupHandlerTests
{
    [Fact]
    public async Task Cleanup_lists_only_unreferenced_images()
    {
        var images = new InMemoryImageRepository();
        var referencedId = await images.NextId();
        await images.Save(AnImage(referencedId, "referenced.jpg"));
        var orphanId = await images.NextId();
        await images.Save(AnImage(orphanId, "orphan.jpg"));

        var articles = new InMemoryArticleRepository();
        await ReferenceImageFromAnArticle(articles, referencedId);

        var env = BuildEnv(images, articles);

        var response = await ImageCleanupHandlers.Cleanup(ARequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Contains("orphan.jpg", response.Body);
        Assert.DoesNotContain("referenced.jpg", response.Body);
    }

    [Fact]
    public async Task DeleteUnused_removes_unreferenced_and_keeps_referenced()
    {
        var images = new InMemoryImageRepository();
        var referencedId = await images.NextId();
        await images.Save(AnImage(referencedId, "referenced.jpg"));
        var orphanId = await images.NextId();
        await images.Save(AnImage(orphanId, "orphan.jpg"));

        var articles = new InMemoryArticleRepository();
        await ReferenceImageFromAnArticle(articles, referencedId);

        var env = BuildEnv(images, articles);

        await ImageCleanupHandlers.DeleteUnused(ARequest())(env);

        var remaining = (await images.List()).Select(i => i.Id.Value).ToList();
        Assert.Contains(referencedId.Value, remaining);
        Assert.DoesNotContain(orphanId.Value, remaining);
    }

    private static async Task ReferenceImageFromAnArticle(IArticleRepository articles, ImageId imageId)
    {
        var id = await articles.NextId();
        await articles.Save(Article.Create(
            id,
            new ArticleTitle("Titel"),
            new ArticleTeaser("Teaser"),
            new ArticleText("Text"),
            new UserId(1),
            DateTimeOffset.UtcNow,
            Option.Some(imageId)));
    }

    private static Image AnImage(ImageId id, string fileName) =>
        Image.Create(id, fileName, ImageContentType.Jpeg, [1, 2, 3], new UserId(1), DateTimeOffset.UtcNow);

    private static Env BuildEnv(IImageRepository images, IArticleRepository articles) => new(
        Articles: articles,
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
        Images: images,
        Pages: new InMemoryPageRepository());

    private static Request ARequest() =>
        new(HttpMethod.Post, "/admin/images/cleanup", Empty, Empty, Empty, Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
