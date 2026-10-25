namespace Foodblog.Test.Identity;

public abstract class PasswordResetTokenStoreContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_token()
    {
        var store = CreateStore();
        var token = AToken("reset1");

        await store.Save(token);

        Assert.Equal(Option.Some(token), await store.Find("reset1"));
    }

    [Fact]
    public async Task Find_returns_none_for_an_unknown_token()
    {
        var store = CreateStore();

        FunctionalAssert.None(await store.Find("unknown"));
    }

    [Fact]
    public async Task Consume_marks_token_as_consumed()
    {
        var store = CreateStore();
        var token = AToken("reset2");

        await store.Save(token);
        await store.Consume("reset2");

        var found = await store.Find("reset2");
        Assert.True(found.Match(none: False, some: t => t.Consumed));
    }

    [Fact]
    public async Task Consume_is_idempotent()
    {
        var store = CreateStore();
        var token = AToken("reset3");

        await store.Save(token);
        await store.Consume("reset3");
        await store.Consume("reset3");

        var found = await store.Find("reset3");
        Assert.True(found.Match(none: False, some: t => t.Consumed));
    }

    protected abstract IPasswordResetTokenStore CreateStore();

    private static PasswordResetToken AToken(string token) =>
        new(token, new UserId(1), DateTimeOffset.UtcNow.AddHours(1), Consumed: false);
}
