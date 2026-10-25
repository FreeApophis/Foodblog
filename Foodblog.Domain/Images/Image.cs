namespace Foodblog.Domain.Images;

public sealed record Image(
    ImageId Id,
    string FileName,
    ImageContentType ContentType,
    byte[] Data,
    int ByteSize,
    UserId UploadedBy,
    DateTimeOffset CreatedAt)
{
    // The crop focal point. Defaults to the centre, reproducing the historical centre-crop, and is
    // adjusted per image from the library. Stored separately from the immutable upload data so it can
    // be retuned without re-encoding the bytes.
    public FocalPoint Focal { get; init; } = FocalPoint.Center;

    public static Image Create(
        ImageId id,
        string fileName,
        ImageContentType contentType,
        byte[] data,
        UserId uploadedBy,
        DateTimeOffset createdAt,
        FocalPoint? focal = null) =>
        new(id, fileName, contentType, data, data.Length, uploadedBy, createdAt)
        {
            Focal = focal ?? FocalPoint.Center,
        };
}
