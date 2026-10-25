namespace Foodblog.Test.Sqlite;

public sealed class SqliteUserRepositoryTests : UserRepositoryContract, IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Save_persists_and_restores_the_avatar_image_id()
    {
        var images = new SqliteImageRepository(_db.Connections);
        var imageId = await images.NextId();
        await images.Save(Image.Create(imageId, "avatar.png", ImageContentType.Png, [0x89, 0x50], new UserId(1), DateTimeOffset.UtcNow));

        var repo = CreateRepository();
        var id = await repo.NextId();
        var user = User.Create(
            id,
            new Email("avatar@blog.de"),
            new DisplayName("Mit Bild"),
            "hash",
            [],
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Option.Some(imageId));

        await repo.Save(user);

        Assert.Equal(Option.Some(user), await repo.FindById(id));
    }

    protected override IUserRepository CreateRepository() => new SqliteUserRepository(_db.Connections);
}
