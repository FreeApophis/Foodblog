using System.Collections.Concurrent;

namespace Foodblog.DataAccess.Identity;

public sealed class InMemoryEmailVerificationTokenStore : IEmailVerificationTokenStore
{
    private readonly ConcurrentDictionary<string, EmailVerificationToken> _tokens = new();

    public ValueTask Save(EmailVerificationToken token)
    {
        _tokens[token.Token] = token;
        return ValueTask.CompletedTask;
    }

    public ValueTask<Option<EmailVerificationToken>> Find(string token) =>
        ValueTask.FromResult(_tokens.GetValueOrNone(token));

    public ValueTask Consume(string token)
    {
        while (_tokens.TryGetValue(token, out var existing))
        {
            if (_tokens.TryUpdate(token, existing with { Consumed = true }, existing))
            {
                break;
            }
        }

        return ValueTask.CompletedTask;
    }
}
