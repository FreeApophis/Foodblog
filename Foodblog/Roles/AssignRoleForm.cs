namespace Foodblog.Roles;

public static class AssignRoleForm
{
    public static DecodedAssignRoleForm Decode(Request request)
    {
        var roles = request.Form
            .Where(kv => kv.Key == "role")
            .Select(kv => kv.Value)
            .ToList();

        return new DecodedAssignRoleForm(roles);
    }
}
