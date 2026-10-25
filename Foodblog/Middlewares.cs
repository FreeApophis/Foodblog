namespace Foodblog;

public static class Middlewares
{
    public static Middleware Recover => next => request => async env =>
    {
        try
        {
            return await next(request)(env);
        }
        catch (Exception ex)
        {
            env.Log.Error(ex);
            var t = env.T;
            return Response.Html(
                Layout.Page(t("error.title"), Html.H1(t("error.heading")) + Html.P(Html.Text(t("error.message"))), env.Ctx),
                500);
        }
    };

    public static Middleware RequestLogging => next => request => async env =>
    {
        var started = env.Clock.Now;
        var response = await next(request)(env);
        var elapsed = env.Clock.Now - started;

        env.Log.Info($"{request.Method} {request.Path} -> {response.Status} in {elapsed.TotalMilliseconds:0.0} ms");

        return response;
    };
}
