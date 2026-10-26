namespace Foodblog.Test;

public sealed class NavViewsTests
{
    [Fact]
    public void Nav_for_guest_contains_login_link()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.Contains("/login", nav);
        Assert.Contains("nav.login", nav);
    }

    [Fact]
    public void Nav_for_guest_contains_register_link()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.Contains("/register", nav);
        Assert.Contains("nav.register", nav);
    }

    [Fact]
    public void Nav_for_authenticated_user_contains_logout_form()
    {
        var user = BuildAuthUser([]);

        var nav = NavViews.Masthead(new ViewContext(user, NoOp, string.Empty));

        Assert.Contains("/logout", nav);
        Assert.Contains("nav.logout", nav);
    }

    [Fact]
    public void Nav_for_authenticated_user_shows_display_name()
    {
        var user = BuildAuthUser([]);

        var nav = NavViews.Masthead(new ViewContext(user, NoOp, string.Empty));

        Assert.Contains("Testbenutzer", nav);
    }

    [Fact]
    public void Nav_keeps_recipes_chip_and_drops_pages_chip()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.Contains("href=\"/recipes\"", nav);
        Assert.DoesNotContain("href=\"/pages\"", nav);
    }

    [Fact]
    public void Nav_for_authenticated_user_uses_person_icon_not_initial()
    {
        var user = BuildAuthUser([]);

        var nav = NavViews.Masthead(new ViewContext(user, NoOp, string.Empty));

        Assert.Contains("is-auth", nav);
        Assert.Contains("M4 21v-1a6 6 0 0 1 6-6h4", nav); // person icon body path
    }

    [Fact]
    public void Nav_renders_user_menu_for_authenticated_user()
    {
        var user = BuildAuthUser([]);

        var nav = NavViews.Masthead(new ViewContext(user, NoOp, string.Empty));

        Assert.Contains("user-menu", nav);
        Assert.Contains("/settings", nav);
    }

    [Fact]
    public void Nav_user_menu_for_guest_offers_login_and_register()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.Contains("user-menu", nav);
        Assert.Contains("/login", nav);
        Assert.Contains("/register", nav);
    }

    [Fact]
    public void Nav_for_admin_contains_admin_link()
    {
        var rule = new PermissionRule("Manage", "user");
        var role = new Role(new RoleId(1), "Admin", [rule]);
        var user = BuildAuthUser([role]);

        var nav = NavViews.Masthead(new ViewContext(user, NoOp, string.Empty));

        Assert.Contains("href=\"/admin\"", nav);
        Assert.Contains("nav.admin", nav);
    }

    [Fact]
    public void Nav_for_non_admin_does_not_contain_admin_link()
    {
        var user = BuildAuthUser([]);

        var nav = NavViews.Masthead(new ViewContext(user, NoOp, string.Empty));

        Assert.DoesNotContain("href=\"/admin\"", nav);
    }

    [Fact]
    public void Language_selector_renders_a_dropdown_with_a_button_per_language()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty, Language: "en"));

        Assert.Contains("lang-menu", nav);
        Assert.Contains("name=\"lang\" value=\"de\"", nav);
        Assert.Contains("Deutsch", nav);
        Assert.Contains("name=\"lang\" value=\"en\"", nav);
        Assert.Contains("English", nav);
    }

    [Fact]
    public void Language_selector_marks_current_language_as_current()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty, Language: "en"));

        Assert.Contains("name=\"lang\" value=\"en\" aria-current=\"true\"", nav);
    }

    [Fact]
    public void Language_selector_does_not_mark_other_languages_as_current()
    {
        var nav = NavViews.Masthead(new ViewContext(Guest.Instance, NoOp, string.Empty, Language: "en"));

        Assert.DoesNotContain("name=\"lang\" value=\"de\" aria-current=\"true\"", nav);
    }

    [Fact]
    public void Utility_bar_shows_the_translated_tagline()
    {
        var bar = NavViews.UtilityBar(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.Contains("NAV.TAGLINE", bar);
        Assert.DoesNotContain("GENIESSEN", bar);
    }

    [Fact]
    public void Utility_bar_shows_the_since_year_when_configured()
    {
        var bar = NavViews.UtilityBar(new ViewContext(Guest.Instance, NoOp, string.Empty, SinceYear: Option.Some(2016)));

        Assert.Contains("NAV.SINCE 2016", bar);
    }

    [Fact]
    public void Utility_bar_omits_the_since_year_when_not_configured()
    {
        var bar = NavViews.UtilityBar(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.DoesNotContain("NAV.SINCE", bar);
        Assert.DoesNotContain("2016", bar);
    }

    [Fact]
    public void Footer_shows_the_current_year_and_the_site_name()
    {
        var footer = NavViews.Footer(new ViewContext(Guest.Instance, NoOp, string.Empty, SiteName: "mein-blog.ch", Year: Option.Some(2031)));

        Assert.Contains("© 2031 MEIN-BLOG.CH", footer);
    }

    [Fact]
    public void Footer_shows_the_translated_made_with_note()
    {
        var footer = NavViews.Footer(new ViewContext(Guest.Instance, NoOp, string.Empty));

        Assert.Contains("FOOTER.MADE_WITH", footer);
        Assert.DoesNotContain("ZÜRICH", footer);
    }

    private static AuthenticatedUser BuildAuthUser(IReadOnlyList<Role> roles)
    {
        var user = User.Create(
            new UserId(1),
            new Email("test@blog.de"),
            new DisplayName("Testbenutzer"),
            "hash",
            [],
            DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, roles);
    }

    private static readonly Translate NoOp = Functional.Identity;
}
