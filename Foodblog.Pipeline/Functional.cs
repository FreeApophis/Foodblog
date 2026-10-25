namespace Foodblog.Pipeline;

public static class Functional
{
    public static App<TEnv, TRequest, TResponse> Compose<TEnv, TRequest, TResponse>(
        App<TEnv, TRequest, TResponse> notFound,
        params Middleware<TEnv, TRequest, TResponse>[] middlewares)
    {
        var app = notFound;

        for (var i = middlewares.Length - 1; i >= 0; i--)
        {
            app = middlewares[i](app);
        }

        return app;
    }
}
