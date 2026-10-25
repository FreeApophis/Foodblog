namespace Foodblog.Test.Roles;

public sealed class AuthenticatedUserTests
{
    [Fact]
    public void IsAuthenticated_returns_true()
    {
        var principal = BuildUser([]);

        Assert.True(principal.IsAuthenticated);
    }

    [Fact]
    public void Can_returns_true_when_role_has_matching_rule()
    {
        var rule = new PermissionRule("Edit", "article");
        var principal = BuildUser([BuildRole([rule])]);

        Assert.True(principal.Can<Edit>(new ArticleResource()));
    }

    [Fact]
    public void Can_returns_false_when_no_matching_rule_exists()
    {
        var principal = BuildUser([BuildRole([])]);

        Assert.False(principal.Can<Edit>(new ArticleResource()));
    }

    [Fact]
    public void Can_returns_false_when_action_matches_but_resource_does_not()
    {
        var rule = new PermissionRule("Edit", "user");
        var principal = BuildUser([BuildRole([rule])]);

        Assert.False(principal.Can<Edit>(new ArticleResource()));
    }

    [Fact]
    public void Can_returns_false_when_resource_matches_but_action_does_not()
    {
        var rule = new PermissionRule("View", "article");
        var principal = BuildUser([BuildRole([rule])]);

        Assert.False(principal.Can<Edit>(new ArticleResource()));
    }

    [Fact]
    public void Can_returns_true_when_permission_exists_in_any_role()
    {
        var rule = new PermissionRule("Edit", "article");
        var principal = BuildUser([BuildRole([]), BuildRole([rule])]);

        Assert.True(principal.Can<Edit>(new ArticleResource()));
    }

    private static AuthenticatedUser BuildUser(IReadOnlyList<Role> roles)
    {
        var user = User.Create(
            new UserId(1),
            new Email("test@blog.de"),
            new DisplayName("Testbenutzer"),
            "hash",
            roles.Select(r => r.Name).ToList(),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        return new AuthenticatedUser(user, roles);
    }

    private static Role BuildRole(IReadOnlyList<PermissionRule> rules) =>
        new(new RoleId(1), "Rolle", rules);
}
