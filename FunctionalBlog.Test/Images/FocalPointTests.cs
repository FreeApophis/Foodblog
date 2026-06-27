namespace FunctionalBlog.Test.Images;

public sealed class FocalPointTests
{
    [Fact]
    public void Center_is_the_middle_of_the_image()
    {
        Assert.Equal(0.5, FocalPoint.Center.X);
        Assert.Equal(0.5, FocalPoint.Center.Y);
        Assert.True(FocalPoint.Center.IsCenter);
    }

    [Fact]
    public void Coordinates_below_zero_are_clamped_to_zero()
    {
        var focal = new FocalPoint(-0.4, -1.0);

        Assert.Equal(0, focal.X);
        Assert.Equal(0, focal.Y);
    }

    [Fact]
    public void Coordinates_above_one_are_clamped_to_one()
    {
        var focal = new FocalPoint(1.3, 2.0);

        Assert.Equal(1, focal.X);
        Assert.Equal(1, focal.Y);
    }

    [Fact]
    public void An_offset_point_is_not_the_center()
    {
        Assert.False(new FocalPoint(0.5, 0.3).IsCenter);
    }

    [Fact]
    public void Image_Create_defaults_to_a_centered_focal_point()
    {
        var image = Image.Create(
            new ImageId(1), "bild.png", ImageContentType.Png, [0x89], new UserId(1), DateTimeOffset.UtcNow);

        Assert.Equal(FocalPoint.Center, image.Focal);
    }
}
