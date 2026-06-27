using System.Data;
using Dapper;

namespace FunctionalBlog.DataAccess.Images;

public sealed class SqliteImageRepository : IImageRepository
{
    private readonly IDbConnection _connection;
    private int _nextId = -1;

    public SqliteImageRepository(IDbConnection connection) => _connection = connection;

    public async ValueTask<ImageId> NextId()
    {
        if (_nextId < 0)
        {
            _nextId = await _connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(id), 0) FROM images");
        }

        return new ImageId(++_nextId);
    }

    public async ValueTask Save(Image image)
    {
        await _connection.ExecuteAsync(
            """
            INSERT OR REPLACE INTO images (id, file_name, content_type, data, byte_size, uploaded_by, created_at, focal_x, focal_y)
            VALUES (@Id, @FileName, @ContentType, @Data, @ByteSize, @UploadedBy, @CreatedAt, @FocalX, @FocalY)
            """,
            new
            {
                Id = image.Id.Value,
                image.FileName,
                ContentType = image.ContentType.Value,
                image.Data,
                image.ByteSize,
                UploadedBy = image.UploadedBy.Value,
                image.CreatedAt,
                FocalX = image.Focal.X,
                FocalY = image.Focal.Y,
            });
    }

    public async ValueTask<Option<Image>> Find(ImageId id)
    {
        var row = await _connection.QuerySingleOrDefaultAsync<ImageRow>(
            "SELECT id AS Id, file_name AS FileName, content_type AS ContentType, data AS Data, byte_size AS ByteSize, uploaded_by AS UploadedBy, created_at AS CreatedAt, focal_x AS FocalX, focal_y AS FocalY FROM images WHERE id = @id",
            new { id = id.Value });

        return Option.FromNullable(row).Select(ToImage);
    }

    public async ValueTask<IReadOnlyList<ImageSummary>> List()
    {
        var rows = await _connection.QueryAsync<ImageSummaryRow>(
            "SELECT id AS Id, file_name AS FileName, content_type AS ContentType, byte_size AS ByteSize, created_at AS CreatedAt, focal_x AS FocalX, focal_y AS FocalY FROM images ORDER BY created_at DESC");
        return rows.Select(ToSummary).ToList();
    }

    public async ValueTask<IReadOnlyDictionary<int, FocalPoint>> FocalPoints()
    {
        var rows = await _connection.QueryAsync<FocalRow>(
            "SELECT id AS Id, focal_x AS FocalX, focal_y AS FocalY FROM images WHERE focal_x <> 0.5 OR focal_y <> 0.5");
        return rows.ToDictionary(r => (int)r.Id, r => new FocalPoint(r.FocalX, r.FocalY));
    }

    public async ValueTask Delete(ImageId id)
    {
        await _connection.ExecuteAsync("DELETE FROM images WHERE id = @id", new { id = id.Value });
    }

    private static Image ToImage(ImageRow row) =>
        new(
            new ImageId((int)row.Id),
            row.FileName,
            ParseContentType(row.ContentType),
            row.Data,
            (int)row.ByteSize,
            new UserId((int)row.UploadedBy),
            row.CreatedAt)
        {
            Focal = new FocalPoint(row.FocalX, row.FocalY),
        };

    private static ImageSummary ToSummary(ImageSummaryRow row) =>
        new(new ImageId((int)row.Id), row.FileName, ParseContentType(row.ContentType), (int)row.ByteSize, row.CreatedAt, new FocalPoint(row.FocalX, row.FocalY));

    private static ImageContentType ParseContentType(string raw) =>
        ImageContentType.ParseOrNone(raw).GetOrElse(ImageContentType.Png);

    private sealed record ImageRow(long Id, string FileName, string ContentType, byte[] Data, long ByteSize, long UploadedBy, DateTimeOffset CreatedAt, double FocalX, double FocalY);

    private sealed record ImageSummaryRow(long Id, string FileName, string ContentType, long ByteSize, DateTimeOffset CreatedAt, double FocalX, double FocalY);

    private sealed record FocalRow(long Id, double FocalX, double FocalY);
}
