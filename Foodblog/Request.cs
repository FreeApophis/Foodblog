namespace Foodblog;

public sealed record Request(
    HttpMethod Method,
    string Path,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Form,
    IReadOnlyDictionary<string, string> Cookies)
{
    public IReadOnlyList<UploadedFile> Files { get; init; } = [];

    // Absolute origin (scheme + host, e.g. "https://foodblog.ch") of the incoming request,
    // used to build absolute URLs for share/SEO metadata. Empty when not running over HTTP.
    public string BaseUrl { get; init; } = string.Empty;
}
