using System.Security.Cryptography;
using System.Text;

namespace Foodblog.DataAccess.Identity;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltBytes = 32;
    private const int KeyBytes = 64;

    public string Hash(string plaintext)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintext),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            KeyBytes);
        return $"$pbkdf2-sha512${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string plaintext, string hash)
    {
        var parts = hash.Split('$');

        if (parts.Length != 5 || parts[1] != "pbkdf2-sha512")
        {
            return false;
        }

        if (!int.TryParse(parts[2], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedKey;

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expectedKey = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintext),
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            KeyBytes);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
