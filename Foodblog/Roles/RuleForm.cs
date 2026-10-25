namespace Foodblog.Roles;

public static class RuleForm
{
    public sealed record Valid(string ActionName, string ResourceKey);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var action = request.Form.GetValueOrNone("action").GetOrElse(string.Empty).Trim();
        var resource = request.Form.GetValueOrNone("resource").GetOrElse(string.Empty).Trim();

        Func<string, string, Valid> create = (a, r) => new Valid(a, r);

        return create
            .Apply(TryParseAction(action), Combine)
            .Apply(TryParseResource(resource), Combine);
    }

    private static Validated<IReadOnlyList<string>, string> TryParseAction(string action) =>
        action.Length > 0
            ? Validated.Succeed<IReadOnlyList<string>, string>(action)
            : Validated.Fail<IReadOnlyList<string>, string>(["admin.roles.error.action_required"]);

    private static Validated<IReadOnlyList<string>, string> TryParseResource(string resource) =>
        resource.Length > 0
            ? Validated.Succeed<IReadOnlyList<string>, string>(resource)
            : Validated.Fail<IReadOnlyList<string>, string>(["admin.roles.error.resource_required"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [.. a, .. b];
}
