namespace Foodblog.Configuration;

// A typed view over the smtp.* configuration values, resolved by ConfigurationCache.
public sealed record SmtpSettings(
    string Host,
    int Port,
    string Username,
    string Password,
    string FromAddress,
    string FromName,
    bool UseSsl)
{
    // Enough is set to attempt a send. Username/password may be blank for unauthenticated relays.
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
