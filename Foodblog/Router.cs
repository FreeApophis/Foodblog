namespace Foodblog;

public static class Router
{
    public static Middleware Create(RouteTable routes) => _ => request => env =>
    {
        var app = routes.Match(request).GetOrElse(NotFound);
        return app(request)(env);
    };

    private static readonly App NotFound = _ => _ => ValueTask.FromResult(Response.NotFound());
}
