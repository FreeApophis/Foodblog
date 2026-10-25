namespace Foodblog;

public sealed class RouteTable
{
    private readonly IReadOnlyList<Route> _routes;

    private RouteTable(IReadOnlyList<Route> routes) => _routes = routes;

    public static RouteTable Empty { get; } = new([]);

    public RouteTable Get(string pattern, App handler) => Add(HttpMethod.Get, pattern, _ => handler);

    public RouteTable Post(string pattern, App handler) => Add(HttpMethod.Post, pattern, _ => handler);

    public RouteTable Get(string pattern, Func<string[], App> factory) => Add(HttpMethod.Get, pattern, factory);

    public RouteTable Post(string pattern, Func<string[], App> factory) => Add(HttpMethod.Post, pattern, factory);

    public Option<App> Match(Request request)
    {
        foreach (var route in _routes)
        {
            if (route.Method != request.Method)
            {
                continue;
            }

            if (TryMatch(route.Segments, request.Path, out var captures))
            {
                return Option.Some(route.Factory(captures));
            }
        }

        return Option<App>.None;
    }

    private RouteTable Add(HttpMethod method, string pattern, Func<string[], App> factory) =>
        new([.._routes, new Route(method, ParsePattern(pattern), factory)]);

    private static string[] ParsePattern(string pattern) =>
        pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static bool TryMatch(string[] segments, string path, out string[] captures)
    {
        captures = [];
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (pathSegments.Length != segments.Length)
        {
            return false;
        }

        var captured = new List<string>();

        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].StartsWith('{') && segments[i].EndsWith('}'))
            {
                captured.Add(pathSegments[i]);
            }
            else if (!string.Equals(segments[i], pathSegments[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        captures = [..captured];
        return true;
    }

    private sealed record Route(HttpMethod Method, string[] Segments, Func<string[], App> Factory);
}
