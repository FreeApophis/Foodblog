using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Identity;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public ValueTask Save(Session session)
    {
        _sessions[session.Token] = session;
        return ValueTask.CompletedTask;
    }

    public ValueTask<Option<Session>> Find(string token) =>
        ValueTask.FromResult(_sessions.GetValueOrNone(token));

    public ValueTask Delete(string token)
    {
        _sessions.TryRemove(token, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteAllForUser(UserId userId)
    {
        foreach (var session in _sessions.Values.Where(s => s.UserId == userId))
        {
            _sessions.TryRemove(session.Token, out _);
        }

        return ValueTask.CompletedTask;
    }
}
