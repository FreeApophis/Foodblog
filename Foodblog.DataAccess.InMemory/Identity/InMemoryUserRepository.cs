using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Identity;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 1;

    public ValueTask<IReadOnlyList<User>> All()
        => ValueTask.FromResult<IReadOnlyList<User>>(_users.Values.ToList());

    public ValueTask<Option<User>> FindById(UserId id)
        => ValueTask.FromResult(_users.GetValueOrNone(id.Value));

    public ValueTask<Option<User>> FindByEmail(Email email)
        => ValueTask.FromResult(_users.Values.FirstOrNone(u => u.Email.Value == email.Value));

    public ValueTask<UserId> NextId() =>
        ValueTask.FromResult(new UserId(Interlocked.Increment(ref _nextId)));

    public ValueTask Save(User user)
    {
        _users[user.Id.Value] = user;
        return ValueTask.CompletedTask;
    }
}
