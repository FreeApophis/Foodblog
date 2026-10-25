namespace Foodblog.Test.Identity;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new Pbkdf2PasswordHasher();

    [Fact]
    public void Hash_returns_a_non_empty_string()
    {
        var hash = _hasher.Hash("geheim");

        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_produces_different_output_on_each_call()
    {
        var first = _hasher.Hash("geheim");
        var second = _hasher.Hash("geheim");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_returns_true_for_correct_password()
    {
        var hash = _hasher.Hash("geheim");

        Assert.True(_hasher.Verify("geheim", hash));
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hash = _hasher.Hash("geheim");

        Assert.False(_hasher.Verify("falsch", hash));
    }

    [Fact]
    public void Verify_returns_false_for_tampered_hash()
    {
        Assert.False(_hasher.Verify("geheim", "tampered$hash$value$xyz"));
    }
}
