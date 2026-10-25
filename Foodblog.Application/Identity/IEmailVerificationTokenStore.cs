namespace Foodblog.Application.Identity;

public interface IEmailVerificationTokenStore
{
    ValueTask Save(EmailVerificationToken token);

    ValueTask<Option<EmailVerificationToken>> Find(string token);

    ValueTask Consume(string token);
}
