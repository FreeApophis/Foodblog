namespace Foodblog.Test.Identity;

public sealed class InMemoryUserRepositoryTests : UserRepositoryContract
{
    protected override IUserRepository CreateRepository() => new InMemoryUserRepository();
}
