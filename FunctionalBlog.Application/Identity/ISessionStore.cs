namespace FunctionalBlog.Application.Identity;

public interface ISessionStore
{
    ValueTask Save(Session session);

    ValueTask<Option<Session>> Find(string token);

    ValueTask Delete(string token);

    ValueTask DeleteAllForUser(UserId userId);
}
