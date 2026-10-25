namespace Foodblog;

public interface ILog
{
    void Info(string message);

    void Error(Exception exception);
}
