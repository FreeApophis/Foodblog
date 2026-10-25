namespace Foodblog.Test.Roles;

public sealed class RoleTests
{
    [Fact]
    public void Create_returns_role_with_no_rules()
    {
        var role = Role.Create(new RoleId(1), "Admin");

        Assert.Empty(role.Rules);
    }

    [Fact]
    public void AddRule_returns_new_role_containing_the_rule()
    {
        var role = Role.Create(new RoleId(1), "Admin");
        var rule = new PermissionRule("Edit", "article");

        var updated = role.AddRule(rule);

        Assert.Contains(rule, updated.Rules);
    }

    [Fact]
    public void AddRule_does_not_mutate_the_original_role()
    {
        var role = Role.Create(new RoleId(1), "Admin");
        var rule = new PermissionRule("Edit", "article");

        role.AddRule(rule);

        Assert.Empty(role.Rules);
    }

    [Fact]
    public void RemoveRule_returns_new_role_without_the_removed_rule()
    {
        var rule = new PermissionRule("Edit", "article");
        var role = Role.Create(new RoleId(1), "Admin").AddRule(rule);

        var updated = role.RemoveRule(rule);

        Assert.Empty(updated.Rules);
    }

    [Fact]
    public void RemoveRule_leaves_other_rules_intact()
    {
        var ruleA = new PermissionRule("Edit", "article");
        var ruleB = new PermissionRule("View", "article");
        var role = Role.Create(new RoleId(1), "Admin").AddRule(ruleA).AddRule(ruleB);

        var updated = role.RemoveRule(ruleA);

        Assert.Single(updated.Rules);
        Assert.Contains(ruleB, updated.Rules);
    }
}
