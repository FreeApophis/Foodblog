namespace Foodblog.Domain.Identity;

public sealed record Email(string Value)
{
    public static Option<Email> ParseOrNone(string raw)
    {
        var normalized = raw.Trim().ToLowerInvariant();
        var at = normalized.IndexOf('@');

        if (at <= 0 || at >= normalized.Length - 2)
        {
            return Option<Email>.None;
        }

        var dot = normalized.LastIndexOf('.');

        if (dot <= at + 1 || dot >= normalized.Length - 1)
        {
            return Option<Email>.None;
        }

        return Option.Some(new Email(normalized));
    }
}
