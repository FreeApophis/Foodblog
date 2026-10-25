namespace Foodblog.Application.Identity;

public interface IUserRepository
{
    ValueTask<IReadOnlyList<User>> All();

    ValueTask<Option<User>> FindById(UserId id);

    ValueTask<Option<User>> FindByEmail(Email email);

    ValueTask<UserId> NextId();

    ValueTask Save(User user);
}
