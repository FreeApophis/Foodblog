namespace Foodblog.Test.Images;

public sealed class FocalIndexTests
{
    [Fact]
    public void FocalStyle_emits_object_position_for_an_off_centre_image()
    {
        var ctx = ContextWith(3, new FocalPoint(0.25, 0.8));

        Assert.Equal(" style=\"object-position:25% 80%\"", ctx.FocalStyle("/images/3"));
    }

    [Fact]
    public void FocalStyle_is_empty_for_a_centred_image()
    {
        var ctx = ContextWith(3, FocalPoint.Center);

        Assert.Equal(string.Empty, ctx.FocalStyle("/images/3"));
    }

    [Fact]
    public void FocalStyle_is_empty_for_an_unknown_image()
    {
        var ctx = ContextWith(3, new FocalPoint(0.1, 0.1));

        Assert.Equal(string.Empty, ctx.FocalStyle("/images/99"));
    }

    [Fact]
    public void FocalStyle_is_empty_for_a_non_image_url()
    {
        var ctx = ContextWith(3, new FocalPoint(0.1, 0.1));

        Assert.Equal(string.Empty, ctx.FocalStyle("/recipes/3"));
    }

    private static ViewContext ContextWith(int imageId, FocalPoint focal) =>
        ViewContext.ForGuest() with
        {
            Focal = new FocalIndex(new Dictionary<int, FocalPoint> { [imageId] = focal }),
        };
}
