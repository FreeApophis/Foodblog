namespace Foodblog.Test.Identity;

public sealed class UserSettingsHandlersTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    [Fact]
    public async Task UpdateAvatar_uploads_the_image_and_sets_it_on_the_user()
    {
        var (env, user) = await BuildEnv();
        var request = new Request(HttpMethod.Post, "/settings/avatar", Empty, Empty, Empty, Empty)
        {
            Files = [new UploadedFile("avatar", "me.png", "image/png", PngBytes)],
        };

        var response = await UserSettingsHandlers.UpdateAvatar(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Single(await env.Images.List());
        Assert.True(await env.Users.FindById(user.Id) is [var saved] && saved.AvatarImageId is [_]);
    }

    [Fact]
    public async Task UpdateAvatar_removes_an_existing_avatar_when_requested()
    {
        var (env, user) = await BuildEnv();
        var imageId = await env.Images.NextId();
        await env.Images.Save(Image.Create(imageId, "old.png", ImageContentType.Png, PngBytes, user.Id, DateTimeOffset.UtcNow));
        await env.Users.Save(user with { AvatarImageId = Option.Some(imageId) });

        var form = new Dictionary<string, string> { ["remove_avatar"] = "on" };
        var request = new Request(HttpMethod.Post, "/settings/avatar", Empty, Empty, form, Empty);

        var response = await UserSettingsHandlers.UpdateAvatar(request)(env);

        Assert.Equal(303, response.Status);
        Assert.True(await env.Users.FindById(user.Id) is [var saved] && saved.AvatarImageId is []);
    }

    [Fact]
    public async Task UpdateAvatar_rejects_a_non_image_upload()
    {
        var (env, user) = await BuildEnv();
        var request = new Request(HttpMethod.Post, "/settings/avatar", Empty, Empty, Empty, Empty)
        {
            Files = [new UploadedFile("avatar", "notes.txt", "text/plain", [0x68, 0x69])],
        };

        var response = await UserSettingsHandlers.UpdateAvatar(request)(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Images.List());
        Assert.True(await env.Users.FindById(user.Id) is [var saved] && saved.AvatarImageId is []);
    }

    private static async Task<(Env Env, User User)> BuildEnv()
    {
        var users = new InMemoryUserRepository();
        var user = User.Create(
            await users.NextId(),
            new Email("anna@blog.de"),
            new DisplayName("Anna"),
            "hash",
            [],
            DateTimeOffset.UtcNow);
        await users.Save(user);

        var env = new Env(
            Articles: new InMemoryArticleRepository(),
            Users: users,
            Roles: new InMemoryRoleRepository(),
            Sessions: new InMemorySessionStore(),
            PasswordResets: new InMemoryPasswordResetTokenStore(),
            PasswordHasher: new Pbkdf2PasswordHasher(),
            Clock: new SystemClock(),
            Log: new ConsoleLog(),
            CurrentUser: new AuthenticatedUser(user, []),
            Recipes: new InMemoryRecipeRepository(),
            Ingredients: new InMemoryIngredientRepository(),
            Units: new InMemoryUnitRepository(),
            Images: new InMemoryImageRepository(),
            Pages: new InMemoryPageRepository());

        return (env, user);
    }
}
