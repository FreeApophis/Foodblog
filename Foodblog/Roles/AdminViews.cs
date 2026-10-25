namespace Foodblog.Roles;

public static class AdminViews
{
    private const string PencilIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>""";

    private const string TrashIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9"><path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2m2 0v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/></svg>""";

    private const string PlusIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14"/></svg>""";

    private const string CheckIcon =
        """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>""";

    private const string XIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M18 6 6 18M6 6l12 12"/></svg>""";

    private static readonly string[] AllActions = ["View", "Create", "Edit", "Delete", "Manage"];

    // Read-only roster styled like the units admin: a single card with a header row and one
    // line per user (name, e-mail, roles). The pencil opens the per-user role editor. Section
    // navigation lives on the /admin dashboard, not here.
    public static string UserList(IReadOnlyList<User> users, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var head = $"""
            <div class="admin-user-head">
                <span>{Html.Encode(t("admin.users.col_user"))}</span>
                <span>{Html.Encode(t("admin.users.col_email"))}</span>
                <span>{Html.Encode(t("admin.users.col_roles"))}</span>
                <span></span>
            </div>
            """;

        var rows = users.Count > 0
            ? string.Concat(users.Select(u => UserRow(u, t)))
            : $"""<p class="admin-user-empty">{Html.Encode(t("admin.users.empty"))}</p>""";

        var section = $"""
            <section class="card admin-users">
                <div class="card-section-head"><h3>{Html.Encode(t("admin.users.title"))}</h3><span class="rule"></span><span class="count">{users.Count}</span></div>
                {head}
                <div class="admin-user-list">{rows}</div>
            </section>
            """;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("admin.users.title")));

        var body = breadcrumb + Html.Raw(section);
        return Layout.Page(t("admin.users.title"), body, ctx);
    }

    public static string UserDetail(User user, IReadOnlyList<Role> allRoles, IReadOnlyList<string> errors, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(Html.Text)));

        HtmlString CheckboxHtml(Role r) =>
            Html.Label(Html.InputCheckbox("role", r.Name, user.RoleNames.Contains(r.Name)) + Html.Raw(" ") + Html.Text(r.Name));

        var roleCheckboxes = HtmlString.Concat(allRoles.Select(CheckboxHtml));
        var formBody = Html.CsrfField(csrfToken) + Html.Fieldset(t("admin.users.roles_label"), roleCheckboxes) + Html.Button(t("admin.users.save"));
        var form = Html.Form($"/admin/users/{user.Id.Value}/roles", formBody);

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Link(t("admin.users.title"), "/admin/users"),
            Crumb.Current(user.Email.Value));

        var body = breadcrumb +
            Html.H1(user.Email.Value) +
            errorHtml +
            form;
        return Layout.Page($"{t("admin.users.detail_title")}: {user.Email.Value}", body, ctx);
    }

    // Overview styled like the ingredient admin: a card of rows (name, rule count) with edit and
    // delete icons. The "new role" call-to-action sits top-right like the recipe index's button.
    public static string RoleList(IReadOnlyList<Role> roles, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var head = $"""
            <div class="unit-head">
                <span>{Html.Encode(t("admin.roles.name"))}</span>
                <span>{Html.Encode(t("admin.roles.rules"))}</span>
                <span></span>
            </div>
            """;

        var rows = roles.Count > 0
            ? string.Concat(roles.Select(r => RoleRow(r, ctx)))
            : $"""<p class="admin-empty">{Html.Encode(t("admin.roles.empty"))}</p>""";

        var section = $"""
            <section class="card unit-editor role-editor">
                {head}
                <div id="roles-list">{rows}</div>
            </section>
            """;

        var newButton = Html.Raw($"""<a class="btn" href="/admin/roles/new">{Html.Encode(t("admin.roles.new"))}</a>""");
        var headRow = Html.Raw($"""<div class="page-head-row"><h1>{Html.Encode(t("admin.roles.title"))}</h1>""") + newButton + Html.Raw("</div>");

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("admin.roles.title")));

        var body = breadcrumb + headRow + Html.Raw(section);
        return Layout.Page(t("admin.roles.title"), body, ctx);
    }

    public static string NewRoleForm(IReadOnlyList<string> errors, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(Html.Text)));

        var formBody = Html.CsrfField(csrfToken) + Html.Label(Html.Text(t("admin.roles.name")) + Html.Input("name")) + Html.Button(t("admin.roles.create"));
        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Link(t("admin.roles.title"), "/admin/roles"),
            Crumb.Current(t("common.new")));
        var body = breadcrumb +
            Html.H1(t("admin.roles.new")) +
            errorHtml +
            Html.Form("/admin/roles", formBody);
        return Layout.Page(t("admin.roles.new"), body, ctx);
    }

    // Rules are managed inline like the units admin: each rule is a row with a trash button (htmx
    // removes it), and a persistent add-row of two selects appends a new rule. Deleting the whole
    // role now lives on the list page, so the detail view is just the rules table.
    public static string RoleDetail(Role role, IReadOnlyList<string> errors, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var head = $"""
            <div class="unit-head">
                <span>{Html.Encode(t("admin.roles.action"))}</span>
                <span>{Html.Encode(t("admin.roles.resource"))}</span>
                <span></span>
            </div>
            """;

        var rows = string.Concat(role.Rules
            .OrderBy(rule => rule.ResourceKey, StringComparer.Ordinal)
            .ThenBy(rule => Array.IndexOf(AllActions, rule.ActionName))
            .Select(rule => RuleRow(role.Id, rule, ctx)));

        var section = $"""
            <section id="rules-section" class="card unit-editor rule-editor">
                <div class="card-section-head"><h3>{Html.Encode(t("admin.roles.rules"))}</h3><span class="rule"></span><span class="count">{role.Rules.Count}</span></div>
                {RulesError(errors, t)}
                {head}
                <div id="rules-list">{rows}</div>
                <button type="button" class="btn-add"
                        hx-get="/admin/roles/{role.Id.Value}/rules/new-row" hx-target="#rules-list" hx-swap="beforeend">
                    {PlusIcon}{Html.Encode(t("admin.roles.add_rule"))}
                </button>
            </section>
            """;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Link(t("admin.roles.title"), "/admin/roles"),
            Crumb.Current(role.Name));

        var body = breadcrumb + Html.H1(role.Name) + Html.Raw(section);
        return Layout.Page($"{t("admin.roles.detail_title")}: {role.Name}", body, ctx);
    }

    // A read-only rule row with an htmx trash button that removes it. The action/resource travel as
    // hidden inputs so the delete request carries them (rules have no id of their own).
    public static string RuleRow(RoleId roleId, PermissionRule rule, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var rowId = RuleRowId(rule);

        return $"""
            <div class="unit-row" id="{rowId}">
                <span class="unit-cell mono" data-label="{Html.Encode(t("admin.roles.action"))}">{Html.Encode(rule.ActionName)}</span>
                <span class="unit-cell mono" data-label="{Html.Encode(t("admin.roles.resource"))}">{Html.Encode(rule.ResourceKey)}</span>
                <span class="unit-actions">
                    <input type="hidden" name="_csrf" value="{Html.Encode(csrfToken)}" />
                    <input type="hidden" name="action" value="{Html.Encode(rule.ActionName)}" />
                    <input type="hidden" name="resource" value="{Html.Encode(rule.ResourceKey)}" />
                    <button type="button" class="icon-remove" title="{Html.Encode(t("admin.roles.remove_rule"))}" aria-label="{Html.Encode(t("admin.roles.remove_rule"))}"
                            hx-post="/admin/roles/{roleId.Value}/rules/delete" hx-target="#{rowId}" hx-swap="outerHTML"
                            hx-include="closest .unit-row">{TrashIcon}</button>
                </span>
            </div>
            """;
    }

    // Out-of-band fragment refreshing the rules section's error banner (empty list clears it).
    public static string RulesError(IReadOnlyList<string> errors, Translate t)
    {
        var content = errors.Count == 0
            ? string.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key))))).Render();
        return $"""<div id="rules-error" class="unit-error" hx-swap-oob="true">{content}</div>""";
    }

    // The inline add-row revealed by the "+ add rule" button: two selects with save/cancel. Saving
    // posts to /rules (replacing this row with the new rule's row, or re-rendering with an error on a
    // duplicate); cancelling removes the row. Mirrors the units admin's inline editor.
    public static string AddRuleRow(RoleId roleId, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var actionOptions = string.Concat(AllActions.Select(a => $"""<option value="{Html.Encode(a)}">{Html.Encode(a)}</option>"""));
        var resourceOptions = string.Concat(Resources.AllKeys.Select(r => $"""<option value="{Html.Encode(r)}">{Html.Encode(r)}</option>"""));

        return $"""
            <div class="unit-row unit-row-edit rule-add-row" id="rule-add-row">
                <input type="hidden" name="_csrf" value="{Html.Encode(csrfToken)}" />
                <select name="action">{actionOptions}</select>
                <select name="resource">{resourceOptions}</select>
                <span class="unit-actions">
                    <button type="button" class="icon-btn" title="{Html.Encode(t("common.save"))}" aria-label="{Html.Encode(t("common.save"))}"
                            hx-post="/admin/roles/{roleId.Value}/rules" hx-target="closest .rule-add-row" hx-swap="outerHTML"
                            hx-include="closest .rule-add-row">{CheckIcon}</button>
                    <button type="button" class="icon-cancel" title="{Html.Encode(t("common.cancel"))}" aria-label="{Html.Encode(t("common.cancel"))}"
                            hx-get="/admin/roles/{roleId.Value}/rules/cancel-new" hx-target="closest .rule-add-row" hx-swap="outerHTML">{XIcon}</button>
                </span>
            </div>
            """;
    }

    // The DOM id of a rule's row, derived from its action+resource (rules carry no numeric id).
    private static string RuleRowId(PermissionRule rule) => $"rule-row-{rule.ActionName}-{rule.ResourceKey}";

    private static string RoleRow(Role role, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var id = role.Id.Value;

        var deleteButton = Html.Raw($"""<button type="submit" class="icon-remove" title="{Html.Encode(t("admin.roles.delete"))}" aria-label="{Html.Encode(t("admin.roles.delete"))}">{TrashIcon}</button>""");
        var deleteForm = Html.Form(
            $"/admin/roles/{id}/delete",
            Html.CsrfField(csrfToken) + deleteButton,
            cssClass: "inline-form",
            confirm: t("common.confirm_delete")).Render();

        return $"""
            <div class="unit-row" id="role-row-{id}">
                <span class="unit-cell" data-label="{Html.Encode(t("admin.roles.name"))}"><a href="/admin/roles/{id}">{Html.Encode(role.Name)}</a></span>
                <span class="unit-cell mono" data-label="{Html.Encode(t("admin.roles.rules"))}">{role.Rules.Count}</span>
                <span class="unit-actions">
                    <a class="icon-btn" href="/admin/roles/{id}" title="{Html.Encode(t("common.edit"))}" aria-label="{Html.Encode(t("common.edit"))}">{PencilIcon}</a>
                    {deleteForm}
                </span>
            </div>
            """;
    }

    private static string UserRow(User u, Translate t)
    {
        var hasRoles = u.RoleNames.Count > 0;
        var roles = hasRoles ? string.Join(", ", u.RoleNames) : t("admin.users.no_roles");
        var rolesClass = hasRoles ? "admin-user-cell" : "admin-user-cell muted";

        return $"""
            <div class="admin-user-row">
                <span class="admin-user-cell" data-label="{Html.Encode(t("admin.users.col_user"))}">{Html.Encode(u.DisplayName.Value)}</span>
                <span class="admin-user-cell mono" data-label="{Html.Encode(t("admin.users.col_email"))}">{Html.Encode(u.Email.Value)}</span>
                <span class="{rolesClass}" data-label="{Html.Encode(t("admin.users.col_roles"))}">{Html.Encode(roles)}</span>
                <span class="unit-actions">
                    <a class="icon-btn" href="/admin/users/{u.Id.Value}" title="{Html.Encode(t("common.edit"))}" aria-label="{Html.Encode(t("common.edit"))}">{PencilIcon}</a>
                </span>
            </div>
            """;
    }
}
