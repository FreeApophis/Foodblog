namespace Foodblog.Test.Roles;

public sealed class GuestTests
{
    [Fact]
    public void IsAuthenticated_returns_false()
    {
        Assert.False(Guest.Instance.IsAuthenticated);
    }

    [Fact]
    public void Can_always_returns_false_for_any_action_and_resource()
    {
        Assert.False(Guest.Instance.Can<View>(new ArticleResource()));
        Assert.False(Guest.Instance.Can<Create>(new ArticleResource()));
        Assert.False(Guest.Instance.Can<Edit>(new ArticleResource()));
        Assert.False(Guest.Instance.Can<Delete>(new ArticleResource()));
        Assert.False(Guest.Instance.Can<Manage>(new UserResource()));
        Assert.False(Guest.Instance.Can<Manage>(new RoleResource()));
        Assert.False(Guest.Instance.Can<Manage>(new RuleResource()));
    }
}
