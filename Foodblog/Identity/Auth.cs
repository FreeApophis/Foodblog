namespace Foodblog.Identity;

public static class Auth
{
    public static App RequireAuth(App inner) => request => env =>
        env.CurrentUser.IsAuthenticated
            ? inner(request)(env)
            : ValueTask.FromResult(Response.Redirect("/login"));

    public static App RequirePermission<TAction>(IResource resource, App inner)
        where TAction : IAction => request => env =>
        env.CurrentUser.Can<TAction>(resource)
            ? inner(request)(env)
            : ValueTask.FromResult(
                env.CurrentUser.IsAuthenticated
                    ? Response.Forbidden(env.Ctx)
                    : Response.Redirect("/login"));
}
