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
    public void A_centred_focal_point_offsets_a_crop_window_to_the_middle()
    {
        Assert.Equal(25, FocalPoint.Center.CropLeft(imageExtent: 100, cropExtent: 50));
        Assert.Equal(25, FocalPoint.Center.CropTop(imageExtent: 100, cropExtent: 50));
    }

    [Fact]
    public void A_focal_point_at_the_top_left_offsets_the_crop_window_to_zero()
    {
        var topLeft = new FocalPoint(0, 0);

        Assert.Equal(0, topLeft.CropLeft(imageExtent: 100, cropExtent: 50));
        Assert.Equal(0, topLeft.CropTop(imageExtent: 100, cropExtent: 50));
    }

    [Fact]
    public void A_focal_point_at_the_bottom_right_offsets_the_crop_window_to_the_far_edge()
    {
        var bottomRight = new FocalPoint(1, 1);

        Assert.Equal(50, bottomRight.CropLeft(imageExtent: 100, cropExtent: 50));
        Assert.Equal(50, bottomRight.CropTop(imageExtent: 100, cropExtent: 50));
    }

    [Fact]
    public void An_offset_focal_point_places_the_crop_window_proportionally()
    {
        Assert.Equal(10, new FocalPoint(0.2, 0.2).CropLeft(imageExtent: 100, cropExtent: 50));
    }

    [Fact]
    public void A_crop_as_large_as_the_image_cannot_move()
    {
        Assert.Equal(0, new FocalPoint(0.8, 0.8).CropLeft(imageExtent: 50, cropExtent: 50));
    }

    [Fact]
    public void Image_Create_defaults_to_a_centered_focal_point()
    {
        var image = Image.Create(
            new ImageId(1), "bild.png", ImageContentType.Png, [0x89], new UserId(1), DateTimeOffset.UtcNow);

        Assert.Equal(FocalPoint.Center, image.Focal);
    }
}
