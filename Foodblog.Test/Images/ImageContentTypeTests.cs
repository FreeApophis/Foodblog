namespace Foodblog.Test.Images;

public class ImageContentTypeTests
{
    [Fact]
    public void Detect_recognises_a_jpeg_by_its_magic_bytes()
    {
        var detected = ImageContentType.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0x00]);

        Assert.Equal(Option.Some(ImageContentType.Jpeg), detected);
    }

    [Fact]
    public void Detect_recognises_a_png_by_its_magic_bytes()
    {
        var detected = ImageContentType.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);

        Assert.Equal(Option.Some(ImageContentType.Png), detected);
    }

    [Fact]
    public void Detect_recognises_a_webp_by_its_riff_and_webp_markers()
    {
        var detected = ImageContentType.Detect(
            [0x52, 0x49, 0x46, 0x46, 0x10, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]);

        Assert.Equal(Option.Some(ImageContentType.Webp), detected);
    }

    [Fact]
    public void Detect_returns_none_for_non_image_content()
    {
        var detected = ImageContentType.Detect([0x25, 0x50, 0x44, 0x46]); // "%PDF"

        FunctionalAssert.None(detected);
    }

    [Fact]
    public void Detect_returns_none_for_an_empty_buffer()
    {
        FunctionalAssert.None(ImageContentType.Detect([]));
    }
}
