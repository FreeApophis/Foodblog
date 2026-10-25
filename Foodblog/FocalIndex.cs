using System.Globalization;

namespace Foodblog;

// Per-request lookup of image focal points for the rendering layer, mirroring SlugIndex. Built once
// per request from IImageRepository (see FocalMiddleware) and threaded into views via ViewContext.
// Holds only off-centre images, so a centred (default) image produces no object-position and keeps
// the browser's centre-crop — exactly the historical behaviour.
public sealed class FocalIndex
{
    private readonly IReadOnlyDictionary<int, FocalPoint> _byId;

    public FocalIndex(IReadOnlyDictionary<int, FocalPoint> byId) => _byId = byId;

    public static FocalIndex Empty { get; } = new(new Dictionary<int, FocalPoint>());

    // The CSS object-position for an image (e.g. "25% 80%"), or None when the image is centred,
    // unknown, or has no registered focal point.
    public Option<string> PositionFor(int imageId) =>
        _byId.GetValueOrNone(imageId)
            .SelectMany(focal => focal.IsCenter ? Option<string>.None : Option.Some(Format(focal)));

    private static string Format(FocalPoint focal) => $"{Percent(focal.X)} {Percent(focal.Y)}";

    private static string Percent(double fraction) =>
        (fraction * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
}
