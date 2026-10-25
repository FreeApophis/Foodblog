namespace Foodblog.Test.Images;

public class ImageUploadFormTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public void DecodeMany_returns_all_valid_uploads()
    {
        var request = ARequest([
            new UploadedFile("images", "a.png", "image/png", PngBytes),
            new UploadedFile("images", "b.jpg", "image/jpeg", JpegBytes),
        ]);

        var uploads = ValidatedAssert.IsSuccess(ImageUploadForm.DecodeMany(request, "images"));

        Assert.Equal(2, uploads.Count);
        Assert.Equal(ImageContentType.Png, uploads[0].ContentType);
        Assert.Equal(ImageContentType.Jpeg, uploads[1].ContentType);
    }

    [Fact]
    public void DecodeMany_with_no_files_succeeds_with_an_empty_list()
    {
        var uploads = ValidatedAssert.IsSuccess(ImageUploadForm.DecodeMany(ARequest([]), "images"));

        Assert.Empty(uploads);
    }

    [Fact]
    public void DecodeMany_fails_when_any_file_is_not_an_image()
    {
        var request = ARequest([
            new UploadedFile("images", "ok.png", "image/png", PngBytes),
            new UploadedFile("images", "bad.exe", "image/png", [0x4D, 0x5A, 0x90]),
        ]);

        var errors = ValidatedAssert.IsFailure(ImageUploadForm.DecodeMany(request, "images"));

        Assert.Contains("image.error.unsupported_type", errors);
    }

    [Fact]
    public void DecodeMany_only_considers_the_named_field()
    {
        var request = ARequest([new UploadedFile("cover", "x.png", "image/png", PngBytes)]);

        var uploads = ValidatedAssert.IsSuccess(ImageUploadForm.DecodeMany(request, "images"));

        Assert.Empty(uploads);
    }

    private static Request ARequest(IReadOnlyList<UploadedFile> files) =>
        new(HttpMethod.Post, "/recipes", Empty, Empty, Empty, Empty) { Files = files };

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
