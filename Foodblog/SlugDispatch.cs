namespace Foodblog;

// Resolves a public {slug} route segment to the owning entity's numeric id and dispatches to the
// id-typed show handler. A slug that is unknown — or registered to a different entity type — is a
// 404 (there is no slug history, so stale slugs simply miss). When no slug registry is configured
// (minimal test envs), the segment is treated as a raw numeric id so id-based routing keeps working.
public static class SlugDispatch
{
    public static App Resolve(string entityType, string segment, Func<int, App> inner) => request => async env =>
    {
        if (env.Slugs is { } slugs)
        {
            return await slugs.FindTarget(segment) is [var target] && target.EntityType == entityType
                ? await inner(target.EntityId)(request)(env)
                : Response.NotFound(env.Ctx);
        }

        return int.TryParse(segment, out var id)
            ? await inner(id)(request)(env)
            : Response.NotFound(env.Ctx);
    };
}
