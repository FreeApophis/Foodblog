namespace Foodblog.Test.Identity;

public sealed class UsersHandlersTests
{
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    [Fact]
    public async Task Index_renders_the_uploaded_avatar_for_a_user_who_has_one()
    {
        var users = new InMemoryUserRepository();
        await users.Save(User.Create(
            await users.NextId(),
            new Email("anna@blog.de"),
            new DisplayName("Anna"),
            "hash",
            [],
            DateTimeOffset.UtcNow,
            Option.Some(new ImageId(42))));

        var response = await UsersHandlers.Index(ARequest())(EnvWith(users));

        Assert.Contains("user-avatar-photo", response.Body);
        Assert.Contains("/images/42", response.Body);
    }

    [Fact]
    public async Task Index_falls_back_to_the_initial_when_no_avatar_is_set()
    {
        var users = new InMemoryUserRepository();
        await users.Save(User.Create(
            await users.NextId(),
            new Email("bert@blog.de"),
            new DisplayName("Bert"),
            "hash",
            [],
            DateTimeOffset.UtcNow));

        var response = await UsersHandlers.Index(ARequest())(EnvWith(users));

        Assert.DoesNotContain("user-avatar-photo", response.Body);
        Assert.Contains("class=\"initial\"", response.Body);
    }

    private static Request ARequest() => new(HttpMethod.Get, "/users", Empty, Empty, Empty, Empty);

    private static Env EnvWith(IUserRepository users) => new(
        Articles: new InMemoryArticleRepository(),
        Users: users,
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
}
