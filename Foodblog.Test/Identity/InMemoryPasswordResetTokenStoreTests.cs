namespace Foodblog.Test.Identity;

public sealed class InMemoryPasswordResetTokenStoreTests : PasswordResetTokenStoreContract
{
    protected override IPasswordResetTokenStore CreateStore() => new InMemoryPasswordResetTokenStore();
}
