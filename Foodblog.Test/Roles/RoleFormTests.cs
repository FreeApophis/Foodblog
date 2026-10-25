namespace Foodblog.Test.Roles;

public sealed class RoleFormTests
{
    [Fact]
    public void Valid_name_returns_success()
    {
        var form = ValidatedAssert.IsSuccess(Decode("Administrator"));

        Assert.Equal("Administrator", form.Name);
    }

    [Fact]
    public void Empty_name_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty));

        Assert.Contains("admin.roles.error.name_required", errors);
    }

    private static Validated<IReadOnlyList<string>, RoleForm.Valid> Decode(string name) =>
        RoleForm.Decode(new Request(
            HttpMethod.Post,
            "/admin/roles",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string> { ["name"] = name },
            new Dictionary<string, string>()));
}
