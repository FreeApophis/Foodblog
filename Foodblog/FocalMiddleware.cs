namespace Foodblog;

// Loads the off-centre image focal points once per request and threads them into Env, so the
// rendering layer can emit object-position synchronously (ViewContext.FocalStyle / FocalIndex).
// Mirrors SlugMiddleware; the query returns only retuned images, so the common cost is near zero.
public static class FocalMiddleware
{
    public static Middleware Create() => next => request => async env =>
        await next(request)(env with { Focal = new FocalIndex(await env.Images.FocalPoints()) });
}
