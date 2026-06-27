namespace FunctionalBlog.Application.Images;

public sealed record ImageSummary(
    ImageId Id,
    string FileName,
    ImageContentType ContentType,
    int ByteSize,
    DateTimeOffset CreatedAt,
    FocalPoint Focal);
