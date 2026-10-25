namespace Foodblog;

public sealed record Response(
    int Status,
    string ContentType,
    IReadOnlyDictionary<string, string> Headers,
    string Body)
{
    public IReadOnlyList<string> SetCookies { get; init; } = [];

    public byte[]? Binary { get; init; }

    public Response WithHeader(string name, string value) =>
        this with { Headers = new Dictionary<string, string>(Headers) { [name] = value } };

    public Response WithCookie(Cookie cookie) =>
        this with { SetCookies = [..SetCookies, cookie.ToHeader()] };

    public static Response Html(string body, int status = 200) =>
        new(status, "text/html; charset=utf-8", EmptyHeaders, body);

    public static Response Text(string body, int status = 200) =>
        new(status, "text/plain; charset=utf-8", EmptyHeaders, body);

    public static Response Css(string body, int status = 200) =>
        new(status, "text/css; charset=utf-8", EmptyHeaders, body);

    public static Response Js(string body, int status = 200) =>
        new(status, "application/javascript; charset=utf-8", EmptyHeaders, body);

    public static Response Redirect(string location) =>
        new(
            303,
            "text/plain; charset=utf-8",
            new Dictionary<string, string> { ["Location"] = location },
            "Redirecting...");

    public static Response Forbidden(ViewContext? ctx = null)
    {
        var resolved = ctx ?? ViewContext.ForGuest();
        var t = resolved.T;
        return Html(Layout.Page(t("error.forbidden_heading"), Foodblog.Html.H1(t("error.forbidden_heading")) + Foodblog.Html.P(Foodblog.Html.Text(t("error.forbidden_message"))), resolved), 403);
    }

    public static Response NotFound(ViewContext? ctx = null)
    {
        var resolved = ctx ?? ViewContext.ForGuest();
        var t = resolved.T;
        return Html(Layout.Page(t("error.notfound_heading"), Foodblog.Html.H1(t("error.notfound_heading")) + Foodblog.Html.P(Foodblog.Html.Text(t("error.notfound_message"))), resolved), 404);
    }

    public static Response Bytes(string contentType, byte[] bytes, IReadOnlyDictionary<string, string>? headers = null) =>
        new(200, contentType, headers ?? EmptyHeaders, string.Empty) { Binary = bytes };

    public static Response JsonDownload(string filename, string body) =>
        new(
            200,
            "application/json; charset=utf-8",
            new Dictionary<string, string> { ["Content-Disposition"] = $"attachment; filename=\"{filename}\"" },
            body);

    private static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
        new Dictionary<string, string>();
}
