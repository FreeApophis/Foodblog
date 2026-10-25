using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FunctionalBlog.Configuration;

// An IEmailSender backed by MailKit. Reads SMTP settings from the live ConfigurationCache on
// each send, so changes made in the admin settings page take effect without a restart.
// Throws on a misconfigured server or a failed send; transactional callers (registration,
// password reset) catch and log so a mail hiccup never 500s the request, while the admin
// "send test email" surfaces the error.
public sealed class SmtpEmailSender : IEmailSender
{
    private const int ImplicitTlsPort = 465;

    private readonly ConfigurationCache _config;
    private readonly ILog _log;

    public SmtpEmailSender(ConfigurationCache config, ILog log)
    {
        _config = config;
        _log = log;
    }

    public async ValueTask Send(string to, string subject, string body)
    {
        var smtp = _config.Smtp;
        if (!smtp.IsConfigured)
        {
            throw new InvalidOperationException("SMTP ist nicht konfiguriert (Host und Absenderadresse fehlen).");
        }

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, SocketOptions(smtp));

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);
        _log.Info($"[E-Mail] gesendet an {to}: \"{subject}\"");
    }

    // "SSL" means TLS is required, never opportunistic: StartTlsWhenAvailable would silently fall back
    // to plain text when an attacker strips the STARTTLS offer, leaking credentials and reset links.
    // Port 465 is implicit TLS (SMTPS); every other port must upgrade with STARTTLS or fail.
    public static SecureSocketOptions SocketOptions(SmtpSettings smtp) =>
        (smtp.UseSsl, smtp.Port) switch
        {
            (false, _) => SecureSocketOptions.None,
            (true, ImplicitTlsPort) => SecureSocketOptions.SslOnConnect,
            (true, _) => SecureSocketOptions.StartTls,
        };
}
