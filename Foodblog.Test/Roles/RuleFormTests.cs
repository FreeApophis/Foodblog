namespace Foodblog.Test.Roles;

public sealed class RuleFormTests
{
    [Fact]
    public void Valid_form_returns_success_with_typed_fields()
    {
        var form = ValidatedAssert.IsSuccess(Decode("view", "articles"));

        Assert.Equal("view", form.ActionName);
        Assert.Equal("articles", form.ResourceKey);
    }

    [Fact]
    public void Empty_action_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, "articles"));

        Assert.Contains("admin.roles.error.action_required", errors);
    }

    [Fact]
    public void Empty_resource_returns_failure()
    {
        var errors = ValidatedAssert.IsFailure(Decode("view", string.Empty));

        Assert.Contains("admin.roles.error.resource_required", errors);
    }

    [Fact]
    public void Both_missing_accumulates_two_errors()
    {
        var errors = ValidatedAssert.IsFailure(Decode(string.Empty, string.Empty));

        Assert.Equal(2, errors.Count);
        Assert.Contains("admin.roles.error.action_required", errors);
        Assert.Contains("admin.roles.error.resource_required", errors);
    }

    private static Validated<IReadOnlyList<string>, RuleForm.Valid> Decode(string action, string resource) =>
        RuleForm.Decode(new Request(
            HttpMethod.Post,
            "/admin/roles/1/rules",
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string> { ["action"] = action, ["resource"] = resource },
            new Dictionary<string, string>()));
}
