namespace Foodblog.Test.Identity;

public abstract class EmailVerificationTokenStoreContract
{
    [Fact]
    public async Task Save_then_Find_returns_the_saved_token()
    {
        var store = CreateStore();
        var token = AToken("verify1");

        await store.Save(token);

        Assert.Equal(Option.Some(token), await store.Find("verify1"));
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

        await store.Save(AToken("verify2"));
        await store.Consume("verify2");

        var found = await store.Find("verify2");
        Assert.True(found.Match(none: False, some: t => t.Consumed));
    }

    [Fact]
    public async Task Consume_is_idempotent()
    {
        var store = CreateStore();

        await store.Save(AToken("verify3"));
        await store.Consume("verify3");
        await store.Consume("verify3");

        var found = await store.Find("verify3");
        Assert.True(found.Match(none: False, some: t => t.Consumed));
    }

    protected abstract IEmailVerificationTokenStore CreateStore();

    private static EmailVerificationToken AToken(string token) =>
        new(token, new UserId(1), DateTimeOffset.UtcNow.AddHours(1), Consumed: false);
}
