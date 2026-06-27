namespace FunctionalBlog.Application.Images;

public interface IImageRepository
{
    ValueTask<ImageId> NextId();

    ValueTask Save(Image image);

    ValueTask<Option<Image>> Find(ImageId id);

    ValueTask<IReadOnlyList<ImageSummary>> List();

    // The focal points that differ from the centre, keyed by image id — for the render layer to emit
    // object-position. Centred images are omitted, so the common case stays a near-empty map.
    ValueTask<IReadOnlyDictionary<int, FocalPoint>> FocalPoints();

    ValueTask Delete(ImageId id);
}
