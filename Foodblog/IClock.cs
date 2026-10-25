namespace Foodblog;

public interface IClock
{
    DateTimeOffset Now { get; }
}
