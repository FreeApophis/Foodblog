namespace Foodblog.Test.Identity;

// Captures sent mail so tests can assert on it and extract the token from a verification or
// password-reset link in the body.
public sealed class RecordingEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];

    public ValueTask Send(string to, string subject, string body)
    {
        Sent.Add((to, subject, body));
        return ValueTask.CompletedTask;
    }

    // The "token=..." value from the most recently sent email's link.
    public string ExtractToken()
    {
        var body = Sent.Count == 0 ? string.Empty : Sent[^1].Body;
        var marker = body.IndexOf("token=", StringComparison.Ordinal);
        return marker < 0 ? string.Empty : body[(marker + "token=".Length)..].Trim();
    }
}
