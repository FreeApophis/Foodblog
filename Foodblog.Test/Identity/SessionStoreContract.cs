namespace Foodblog.Test.Identity;

public abstract class SessionStoreContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_session()
    {
        var store = CreateStore();
        var session = ASession("tok1");

        await store.Save(session);

        Assert.Equal(Option.Some(session), await store.Find("tok1"));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_token()
    {
        var store = CreateStore();

        FunctionalAssert.None(await store.Find("unknown"));
    }

    [Fact]
    public async Task Delete_removes_the_session()
    {
        var store = CreateStore();
        var session = ASession("tok2");

        await store.Save(session);
        await store.Delete("tok2");

        FunctionalAssert.None(await store.Find("tok2"));
    }

    [Fact]
    public async Task DeleteAllForUser_removes_only_that_users_sessions()
    {
        var store = CreateStore();

        await store.Save(ASession("tok3", userId: 1));
        await store.Save(ASession("tok4", userId: 1));
        await store.Save(ASession("tok5", userId: 2));
        await store.DeleteAllForUser(new UserId(1));

        FunctionalAssert.None(await store.Find("tok3"));
        FunctionalAssert.None(await store.Find("tok4"));
        FunctionalAssert.Some(await store.Find("tok5"));
    }

    protected abstract ISessionStore CreateStore();

    private static Session ASession(string token, int userId = 1) =>
        new(token, new UserId(userId), DateTimeOffset.UtcNow.AddDays(30));
}
