namespace Foodblog.Application.Identity;

public interface IEmailSender
{
    ValueTask Send(string to, string subject, string body);
}
