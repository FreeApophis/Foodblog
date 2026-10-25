namespace Foodblog;

public sealed class ConsoleLog : ILog
{
    public void Info(string message) => Console.WriteLine(message);

    public void Error(Exception exception) => Console.Error.WriteLine(exception);
}
