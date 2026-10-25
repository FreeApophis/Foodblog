namespace Foodblog.Test.Identity;

public sealed class InMemoryEmailVerificationTokenStoreTests : EmailVerificationTokenStoreContract
{
    protected override IEmailVerificationTokenStore CreateStore() => new InMemoryEmailVerificationTokenStore();
}
