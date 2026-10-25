namespace Foodblog.Test.Pages;

public class PageSeoTests
{
    [Fact]
    public void Build_sets_share_metadata_from_the_page()
    {
        var page = Page.Create(
            new PageId(4),
            new PageTitle("Impressum"),
            new PageContent("Verantwortlich: [b]Anna[/b]."));

        var meta = PageSeo.Build(page, "https://foodblog.ch");

        Assert.Equal("website", meta.Type);
        Assert.Equal("https://foodblog.ch/pages/4", meta.Url);
        Assert.Contains("Verantwortlich: Anna", meta.Description);
        Assert.DoesNotContain("[b]", meta.Description);
        Assert.Equal(string.Empty, meta.ImageUrl);
    }

    [Fact]
    public void Build_uses_the_slug_for_the_canonical_url_when_supplied()
    {
        var page = Page.Create(new PageId(4), new PageTitle("Impressum"), new PageContent("Inhalt."));

        var meta = PageSeo.Build(page, "https://foodblog.ch", "impressum");

        Assert.Equal("https://foodblog.ch/pages/impressum", meta.Url);
    }
}
