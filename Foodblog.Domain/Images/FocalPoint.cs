namespace Foodblog.Domain.Images;

// Where the visually important part of an image sits, as fractions of its width and height
// (0..1, top-left origin). Used to drive CSS object-position so that a cover-cropped image keeps
// its subject in frame instead of being sliced through the centre. Center is the neutral default
// that reproduces the historical centre-crop behaviour.
public sealed record FocalPoint
{
    public FocalPoint(double x, double y)
    {
        X = Clamp(x);
        Y = Clamp(y);
    }

    public double X { get; }

    public double Y { get; }

    public static FocalPoint Center { get; } = new(0.5, 0.5);

    public bool IsCenter => X == 0.5 && Y == 0.5;

    // The horizontal top-left pixel offset of a cover-crop window of width cropExtent within an image
    // of width imageExtent, so that this focal point sits at the matching spot. Clamped to keep the
    // window inside the image. The CSS object-position equivalent for server-side (e.g. PDF) cropping.
    public int CropLeft(int imageExtent, int cropExtent) => Offset(X, imageExtent, cropExtent);

    // The vertical counterpart of CropLeft.
    public int CropTop(int imageExtent, int cropExtent) => Offset(Y, imageExtent, cropExtent);

    private static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

    private static int Offset(double fraction, int imageExtent, int cropExtent)
    {
        var max = imageExtent - cropExtent;
        if (max <= 0)
        {
            return 0;
        }

        var offset = (int)Math.Round(fraction * max, MidpointRounding.AwayFromZero);
        return offset < 0 ? 0 : offset > max ? max : offset;
    }
}
