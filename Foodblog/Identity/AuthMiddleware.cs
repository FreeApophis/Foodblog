namespace Foodblog.Identity;

public static class AuthMiddleware
{
    public static Middleware Create() => next => request => async env =>
    {
        var principal = await ResolvePrincipal(request, env);
        return await next(request)(env with { CurrentUser = principal });
    };

    public static async ValueTask<IPrincipal> ResolvePrincipal(Request request, Env env)
    {
        if (!request.Cookies.TryGetValue("session", out var token))
        {
            return Guest.Instance;
        }

        if ((await env.Sessions.Find(token)) is not [var session] || session.ExpiresAt <= env.Clock.Now)
        {
            return Guest.Instance;
        }

        if ((await env.Users.FindById(session.UserId)) is not [var user])
        {
            return Guest.Instance;
        }

        var allRoles = await env.Roles.All();
        var userRoles = allRoles.Where(r => user.RoleNames.Contains(r.Name)).ToList();
        return new AuthenticatedUser(user, userRoles);
    }
}
