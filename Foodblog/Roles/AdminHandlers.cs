namespace Foodblog.Roles;

public static class AdminHandlers
{
    public static App UserList => _ => async env =>
    {
        var users = await env.Users.All();
        return Response.Html(AdminViews.UserList(users, env.Ctx));
    };

    public static App UserDetail(int userId) => _ => async env =>
    {
        if ((await env.Users.FindById(new UserId(userId))) is [var user])
        {
            var roles = await env.Roles.All();
            return Response.Html(AdminViews.UserDetail(user, roles, [], env.Ctx));
        }

        return Response.NotFound(env.Ctx);
    };

    public static App UpdateUserRoles(int userId) => request => async env =>
    {
        if ((await env.Users.FindById(new UserId(userId))) is [var user])
        {
            var decoded = AssignRoleForm.Decode(request);
            await env.Users.Save(user with { RoleNames = decoded.RoleNames });
            return Response.Redirect($"/admin/users/{userId}");
        }

        return Response.NotFound(env.Ctx);
    };

    public static App RoleList => _ => async env =>
    {
        var roles = await env.Roles.All();
        return Response.Html(AdminViews.RoleList(roles, env.Ctx));
    };

    public static App NewRoleForm => _ => env =>
        ValueTask.FromResult(Response.Html(AdminViews.NewRoleForm([], env.Ctx)));

    public static App CreateRole => request => async env =>
        await RoleForm.Decode(request).Match(
            failure: f => Task.FromResult(Response.Html(AdminViews.NewRoleForm(f.Error, env.Ctx), 400)),
            success: async s =>
            {
                var id = await env.Roles.NextId();
                await env.Roles.Save(Role.Create(id, s.Value.Name));
                return Response.Redirect("/admin/roles");
            });

    public static App RoleDetail(int roleId) => _ => async env =>
    {
        if ((await env.Roles.FindById(new RoleId(roleId))) is [var role])
        {
            return Response.Html(AdminViews.RoleDetail(role, [], env.Ctx));
        }

        return Response.NotFound(env.Ctx);
    };

    // htmx: opens the inline add-row at the end of the rules table.
    public static App NewRuleRow(int roleId) => _ => async env =>
        (await env.Roles.FindById(new RoleId(roleId))) is [var role]
            ? Response.Html(AdminViews.AddRuleRow(role.Id, env.Ctx))
            : Response.Html(string.Empty);

    // htmx: cancelling the not-yet-saved add-row simply removes it (empty swap).
    public static App CancelNewRule => _ => _ =>
        ValueTask.FromResult(Response.Html(string.Empty));

    // htmx: the add-row's save replaces itself (outerHTML) with the new rule's row on success, or
    // re-renders the add-row plus an error banner when the rule is a duplicate or invalid.
    public static App AddRule(int roleId) => request => async env =>
        await (await env.Roles.FindById(new RoleId(roleId))).Match(
            none: () => Task.FromResult(Response.NotFound(env.Ctx)),
            some: async role =>
                await RuleForm.Decode(request).Match(
                    failure: f => Task.FromResult(Response.Html(AdminViews.AddRuleRow(role.Id, env.Ctx) + AdminViews.RulesError(f.Error, env.T))),
                    success: async s =>
                    {
                        var rule = new PermissionRule(s.Value.ActionName, s.Value.ResourceKey);

                        if (role.Rules.Contains(rule))
                        {
                            return Response.Html(AdminViews.AddRuleRow(role.Id, env.Ctx) + AdminViews.RulesError(["admin.roles.rule_exists"], env.T));
                        }

                        await env.Roles.Save(role.AddRule(rule));
                        return Response.Html(AdminViews.RuleRow(role.Id, rule, env.Ctx) + AdminViews.RulesError([], env.T));
                    }));

    // htmx: removes the rule's row (outerHTML swap with an empty body) and clears the error banner.
    public static App DeleteRule(int roleId) => request => async env =>
        await (await env.Roles.FindById(new RoleId(roleId))).Match(
            none: () => Task.FromResult(Response.NotFound(env.Ctx)),
            some: async role =>
            {
                var action = request.Form.GetValueOrNone("action").GetOrElse(string.Empty);
                var resource = request.Form.GetValueOrNone("resource").GetOrElse(string.Empty);
                await env.Roles.Save(role.RemoveRule(new PermissionRule(action, resource)));
                return Response.Html(AdminViews.RulesError([], env.T));
            });

    public static App DeleteRole(int roleId) => _ => async env =>
    {
        if ((await env.Roles.FindById(new RoleId(roleId))) is [var role])
        {
            await env.Roles.Delete(new RoleId(roleId));
            return Response.Redirect("/admin/roles");
        }

        return Response.NotFound(env.Ctx);
    };
}
