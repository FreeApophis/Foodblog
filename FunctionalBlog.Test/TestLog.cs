namespace FunctionalBlog.Test;

public sealed class TestLog : ILog
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public void Info(string message) => _messages.Add(message);

    public void Error(Exception exception) => _messages.Add(exception.ToString());

    public string ExtractResetToken()
    {
        if (_messages.FirstOrNone(m => m.Contains("reset-token:")) is [var line])
        {
            var marker = "reset-token:";
            var start = line.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            return line[start..].Trim().Split(' ')[0];
        }

        throw new InvalidOperationException("No reset token found in log messages.");
    }
}
