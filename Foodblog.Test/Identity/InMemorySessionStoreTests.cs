namespace Foodblog.Test.Identity;

public sealed class InMemorySessionStoreTests : SessionStoreContract
{
    protected override ISessionStore CreateStore() => new InMemorySessionStore();
}
