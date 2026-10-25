namespace Foodblog.Roles;

public static class RoleForm
{
    public sealed record Valid(string Name);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var name = request.Form.GetValueOrNone("name").GetOrElse(string.Empty).Trim();

        return name.Length > 0
            ? Validated.Succeed<IReadOnlyList<string>, Valid>(new Valid(name))
            : Validated.Fail<IReadOnlyList<string>, Valid>(["admin.roles.error.name_required"]);
    }
}
