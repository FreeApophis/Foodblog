using System.Text.RegularExpressions;

namespace Foodblog.Test.Images;

public sealed class ImageViewsTests
{
    [Fact]
    public void Library_has_no_inline_event_handlers()
    {
        var html = ImageViews.Library([AnImage()], ViewContext.ForGuest());

        Assert.DoesNotMatch(new Regex(@"\son[a-z]+\s*=", RegexOptions.IgnoreCase), html);
    }

    [Fact]
    public void Library_embed_snippet_selects_itself_via_the_external_script()
    {
        var html = ImageViews.Library([AnImage()], ViewContext.ForGuest());

        Assert.Contains("data-select-on-click", html);
    }

    private static ImageSummary AnImage() =>
        new(new ImageId(7), "kuchen.png", ImageContentType.Png, 1024, DateTimeOffset.UtcNow, FocalPoint.Center);
}
