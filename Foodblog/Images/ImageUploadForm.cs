namespace Foodblog.Images;

public static class ImageUploadForm
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public sealed record Valid(string FileName, ImageContentType ContentType, byte[] Content);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request) =>
        request.Files.FirstOrNone(f => f.FieldName == "file") is [var file] && file.Content.Length > 0
            ? Validate(file)
            : Validated.Fail<IReadOnlyList<string>, Valid>(["image.error.missing"]);

    // For forms where an image is an optional attachment (e.g. an article cover):
    // None when no file was supplied, an accumulated failure when one was supplied but invalid.
    public static Validated<IReadOnlyList<string>, Option<Valid>> DecodeOptional(Request request, string fieldName)
    {
        if (request.Files.FirstOrNone(f => f.FieldName == fieldName) is not [var file] || file.Content.Length == 0)
        {
            return Validated.Succeed<IReadOnlyList<string>, Option<Valid>>(Option<Valid>.None);
        }

        Func<ImageContentType, byte[], Option<Valid>> create =
            (contentType, content) => Option.Some(new Valid(SanitizeFileName(file.FileName), contentType, content));

        return create
            .Apply(TryContentType(file.Content), Combine)
            .Apply(TrySize(file.Content), Combine);
    }

    // For forms that accept several images at once (e.g. a recipe gallery): validates every
    // supplied file in the named field, accumulating all errors; an empty list when none were sent.
    public static Validated<IReadOnlyList<string>, IReadOnlyList<Valid>> DecodeMany(Request request, string fieldName)
    {
        var files = request.Files.Where(f => f.FieldName == fieldName && f.Content.Length > 0).ToList();

        var failures = new List<string>();
        var valids = new List<Valid>();

        foreach (var file in files)
        {
            switch (Validate(file))
            {
                case Validated<IReadOnlyList<string>, Valid>.Success(var value):
                    valids.Add(value);
                    break;
                case Validated<IReadOnlyList<string>, Valid>.Failure(var error):
                    failures.AddRange(error);
                    break;
            }
        }

        return failures.Count > 0
            ? Validated.Fail<IReadOnlyList<string>, IReadOnlyList<Valid>>(failures)
            : Validated.Succeed<IReadOnlyList<string>, IReadOnlyList<Valid>>(valids);
    }

    private static Validated<IReadOnlyList<string>, Valid> Validate(UploadedFile file)
    {
        Func<ImageContentType, byte[], Valid> create =
            (contentType, content) => new Valid(SanitizeFileName(file.FileName), contentType, content);

        return create
            .Apply(TryContentType(file.Content), Combine)
            .Apply(TrySize(file.Content), Combine);
    }

    private static Validated<IReadOnlyList<string>, ImageContentType> TryContentType(byte[] content) =>
        ImageContentType.Detect(content) switch
        {
            [var contentType] => Validated.Succeed<IReadOnlyList<string>, ImageContentType>(contentType),
            [] => Validated.Fail<IReadOnlyList<string>, ImageContentType>(["image.error.unsupported_type"]),
            _ => throw new InvalidOperationException("Option matches at most one element."),
        };

    private static Validated<IReadOnlyList<string>, byte[]> TrySize(byte[] content) =>
        content.Length <= MaxBytes
            ? Validated.Succeed<IReadOnlyList<string>, byte[]>(content)
            : Validated.Fail<IReadOnlyList<string>, byte[]>(["image.error.too_large"]);

    private static string SanitizeFileName(string fileName)
    {
        var trimmed = Path.GetFileName(fileName).Trim();
        return string.IsNullOrEmpty(trimmed) ? "bild" : trimmed;
    }

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [..a, ..b];
}
