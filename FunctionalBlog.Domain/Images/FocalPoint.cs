namespace FunctionalBlog.Domain.Images;

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

    private static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
