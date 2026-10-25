namespace Foodblog.Application.Identity;

public interface IPasswordHasher
{
    string Hash(string plaintext);

    bool Verify(string plaintext, string hash);
}
