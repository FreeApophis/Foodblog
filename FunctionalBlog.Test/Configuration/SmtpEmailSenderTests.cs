using MailKit.Security;

namespace FunctionalBlog.Test.Configuration;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public void Ssl_on_port_465_uses_implicit_tls()
    {
        Assert.Equal(SecureSocketOptions.SslOnConnect, SmtpEmailSender.SocketOptions(Settings(port: 465, useSsl: true)));
    }

    [Theory]
    [InlineData(587)]
    [InlineData(25)]
    [InlineData(2525)]
    public void Ssl_on_other_ports_requires_starttls(int port)
    {
        Assert.Equal(SecureSocketOptions.StartTls, SmtpEmailSender.SocketOptions(Settings(port, useSsl: true)));
    }

    [Theory]
    [InlineData(465)]
    [InlineData(587)]
    public void Disabled_ssl_uses_no_tls(int port)
    {
        Assert.Equal(SecureSocketOptions.None, SmtpEmailSender.SocketOptions(Settings(port, useSsl: false)));
    }

    private static SmtpSettings Settings(int port, bool useSsl) =>
        new("smtp.example.org", port, "user", "secret", "blog@example.org", "Blog", useSsl);
}
