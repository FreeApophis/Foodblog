namespace Foodblog.Application.Identity;

public interface IPasswordResetTokenStore
{
    ValueTask Save(PasswordResetToken token);

    ValueTask<Option<PasswordResetToken>> Find(string token);

    ValueTask Consume(string token);
}
