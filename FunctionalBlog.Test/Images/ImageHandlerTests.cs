namespace FunctionalBlog.Test.Images;

public sealed class ImageHandlerTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02];

    [Fact]
    public async Task Serve_returns_the_image_bytes_for_an_existing_image()
    {
        var env = BuildEnv();
        var id = await env.Images.NextId();
        await env.Images.Save(Image.Create(id, "katze.png", ImageContentType.Png, PngBytes, new UserId(1), env.Clock.Now));

        var response = await ImageHandlers.Serve(id)(ARequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Equal("image/png", response.ContentType);
        Assert.Equal(PngBytes, response.Binary);
        Assert.Contains("max-age", response.Headers["Cache-Control"]);
    }

    [Fact]
    public async Task Serve_returns_404_for_an_unknown_image()
    {
        var env = BuildEnv();

        var response = await ImageHandlers.Serve(new ImageId(987_654))(ARequest())(env);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Upload_saves_a_valid_image_and_redirects()
    {
        var env = BuildEnv();
        var request = ARequest() with { Files = [new UploadedFile("file", "katze.png", "image/png", PngBytes)] };

        var response = await ImageHandlers.Upload(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/images", response.Headers["Location"]);
        var saved = Assert.Single(await env.Images.List());
        Assert.Equal("katze.png", saved.FileName);
        Assert.Equal(ImageContentType.Png, saved.ContentType);
    }

    [Fact]
    public async Task Upload_rejects_a_non_image_file()
    {
        var env = BuildEnv();
        var request = ARequest() with { Files = [new UploadedFile("file", "schad.exe", "image/png", [0x4D, 0x5A, 0x90])] };

        var response = await ImageHandlers.Upload(request)(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Images.List());
    }

    [Fact]
    public async Task Upload_rejects_a_request_without_a_file()
    {
        var env = BuildEnv();

        var response = await ImageHandlers.Upload(ARequest())(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Images.List());
    }

    [Fact]
    public async Task SetFocal_persists_the_focal_point_and_redirects()
    {
        var env = BuildEnv();
        var id = await env.Images.NextId();
        await env.Images.Save(Image.Create(id, "katze.png", ImageContentType.Png, PngBytes, new UserId(1), env.Clock.Now));
        var form = new Dictionary<string, string> { ["focal_x"] = "0.25", ["focal_y"] = "0.8" };
        var request = new Request(HttpMethod.Post, $"/images/{id.Value}/focal", Empty, Empty, form, Empty);

        var response = await ImageHandlers.SetFocal(id)(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/images", response.Headers["Location"]);
        var saved = FunctionalAssert.Some(await env.Images.Find(id));
        Assert.Equal(0.25, saved.Focal.X);
        Assert.Equal(0.8, saved.Focal.Y);
    }

    [Fact]
    public async Task SetFocal_returns_404_for_an_unknown_image()
    {
        var env = BuildEnv();
        var form = new Dictionary<string, string> { ["focal_x"] = "0.5", ["focal_y"] = "0.5" };
        var request = new Request(HttpMethod.Post, "/images/999/focal", Empty, Empty, form, Empty);

        var response = await ImageHandlers.SetFocal(new ImageId(999))(request)(env);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Delete_removes_the_image_and_redirects()
    {
        var env = BuildEnv();
        var id = await env.Images.NextId();
        await env.Images.Save(Image.Create(id, "katze.png", ImageContentType.Png, PngBytes, new UserId(1), env.Clock.Now));

        var response = await ImageHandlers.Delete(id)(ARequest())(env);

        Assert.Equal(303, response.Status);
        FunctionalAssert.None(await env.Images.Find(id));
    }

    private static Request ARequest() =>
        new(HttpMethod.Post, "/images", Empty, Empty, Empty, Empty);

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
