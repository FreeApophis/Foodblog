using System.Collections.Concurrent;

namespace FunctionalBlog.DataAccess.Images;

public sealed class InMemoryImageRepository : IImageRepository
{
    private readonly ConcurrentDictionary<int, Image> _images = new();
    private int _nextId;

    public ValueTask<ImageId> NextId() =>
        ValueTask.FromResult(new ImageId(Interlocked.Increment(ref _nextId)));

    public ValueTask Save(Image image)
    {
        _images[image.Id.Value] = image;
        return ValueTask.CompletedTask;
    }

    public ValueTask<Option<Image>> Find(ImageId id) =>
        ValueTask.FromResult(_images.GetValueOrNone(id.Value));

    public ValueTask<IReadOnlyList<ImageSummary>> List() =>
        ValueTask.FromResult<IReadOnlyList<ImageSummary>>(
            _images.Values
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ImageSummary(x.Id, x.FileName, x.ContentType, x.ByteSize, x.CreatedAt, x.Focal))
                .ToList());

    public ValueTask<IReadOnlyDictionary<int, FocalPoint>> FocalPoints() =>
        ValueTask.FromResult<IReadOnlyDictionary<int, FocalPoint>>(
            _images.Values
                .Where(x => !x.Focal.IsCenter)
                .ToDictionary(x => x.Id.Value, x => x.Focal));

    public ValueTask Delete(ImageId id)
    {
        _images.TryRemove(id.Value, out _);
        return ValueTask.CompletedTask;
    }
}
