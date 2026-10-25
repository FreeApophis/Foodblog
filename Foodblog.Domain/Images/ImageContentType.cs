namespace Foodblog.Domain.Images;

public sealed record ImageContentType
{
    public static readonly ImageContentType Jpeg = new("image/jpeg");
    public static readonly ImageContentType Png = new("image/png");
    public static readonly ImageContentType Gif = new("image/gif");
    public static readonly ImageContentType Webp = new("image/webp");

    private ImageContentType(string value) => Value = value;

    public string Value { get; }

    // Sniffs the leading "magic" bytes so we trust the actual content over a
    // client-supplied content-type header. Returns None for anything we don't accept.
    public static Option<ImageContentType> Detect(byte[] bytes)
        => StartsWith(bytes, 0xFF, 0xD8, 0xFF)
            ? Option.Some(Jpeg)
            : StartsWith(bytes, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
                ? Option.Some(Png)
                : StartsWith(bytes, 0x47, 0x49, 0x46, 0x38)
                    ? Option.Some(Gif)
                    : IsWebp(bytes)
                        ? Option.Some(Webp)
                        : Option<ImageContentType>.None;

    public static Option<ImageContentType> ParseOrNone(string raw) =>
        new[] { Jpeg, Png, Gif, Webp }.FirstOrNone(t => t.Value == raw);

    private static bool StartsWith(byte[] bytes, params byte[] prefix)
    {
        if (bytes.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (bytes[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }

    // RIFF........WEBP — "RIFF" at offset 0 and "WEBP" at offset 8.
    private static bool IsWebp(byte[] bytes) =>
        bytes.Length >= 12
        && StartsWith(bytes, 0x52, 0x49, 0x46, 0x46)
        && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
}
