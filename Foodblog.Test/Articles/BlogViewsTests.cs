namespace Foodblog.Test.Articles;

public sealed class BlogViewsTests
{
    private static readonly Translate NoOp = key => key;

    [Fact]
    public void Show_renders_the_author_with_an_avatar_in_a_meta_row()
    {
        var html = BlogViews.Show(AnArticle(), "Anna", new ViewContext(Guest.Instance, NoOp, string.Empty), string.Empty);

        Assert.Contains("recipe-meta", html);
        Assert.Contains("class=\"avatar\">A<", html); // initial of "Anna"
        Assert.Contains("Anna", html);
    }

    [Fact]
    public void Show_renders_the_teaser_in_its_own_styled_paragraph()
    {
        var html = BlogViews.Show(AnArticle(), "Anna", new ViewContext(Guest.Instance, NoOp, string.Empty), string.Empty);

        Assert.Contains("<p class=\"blog-teaser\">Ein ausreichend langer Teaser.</p>", html);
    }

    [Fact]
    public void Show_no_longer_uses_the_plain_by_line()
    {
        var html = BlogViews.Show(AnArticle(), "Anna", new ViewContext(Guest.Instance, NoOp, string.Empty), string.Empty);

        Assert.DoesNotContain("article.by", html);
    }

    [Fact]
    public void Show_renders_edit_and_delete_as_round_icon_buttons_when_permitted()
    {
        var principal = UserWith(new PermissionRule("Edit", "article"), new PermissionRule("Delete", "article"));

        var html = BlogViews.Show(AnArticle(), "Anna", new ViewContext(principal, NoOp, string.Empty), string.Empty);

        Assert.Contains("recipe-meta-actions", html);
        Assert.Contains("icon-round", html);
        Assert.Contains("icon-round-danger", html);
        Assert.Contains("/articles/1/edit", html);
        Assert.Contains("/articles/1/delete", html);
    }

    [Fact]
    public void Show_omits_edit_and_delete_for_guests()
    {
        var html = BlogViews.Show(AnArticle(), "Anna", new ViewContext(Guest.Instance, NoOp, string.Empty), string.Empty);

        Assert.DoesNotContain("icon-round", html);
        Assert.DoesNotContain("/articles/1/edit", html);
        Assert.DoesNotContain("/articles/1/delete", html);
    }

    private static Article AnArticle() => Article.Create(
        new ArticleId(1),
        new ArticleTitle("Mein Titel"),
        new ArticleTeaser("Ein ausreichend langer Teaser."),
        new ArticleText("Ein ausreichend langer Text."),
        new UserId(1),
        DateTimeOffset.Parse("2026-01-15T10:00:00Z"),
        Option<ImageId>.None);

    private static AuthenticatedUser UserWith(params PermissionRule[] rules)
    {
        var user = User.Create(
            new UserId(1),
            new Email("autor@blog.de"),
            new DisplayName("Autor"),
            "hash",
            [],
            DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, [new Role(new RoleId(1), "Redaktion", rules)]);
    }
}
