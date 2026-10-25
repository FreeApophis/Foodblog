using Foodblog;

public class LayoutMetaTests
{
    [Fact]
    public void Page_without_meta_emits_no_open_graph_tags()
    {
        var html = Layout.Page("Titel", Html.Text("body"), ViewContext.ForGuest());

        Assert.DoesNotContain("og:", html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
    }

    [Fact]
    public void Page_with_meta_emits_open_graph_twitter_and_canonical()
    {
        var meta = new PageMeta
        {
            Description = "Ein \"feiner\" Kuchen",
            ImageUrl = "https://foodblog.ch/images/7",
            Url = "https://foodblog.ch/recipes/7",
            Type = "article",
        };

        var html = Layout.Page("Apfelkuchen", Html.Text("body"), ViewContext.ForGuest(), meta);

        Assert.Contains("<meta property=\"og:type\" content=\"article\" />", html);
        Assert.Contains("<meta property=\"og:title\" content=\"Apfelkuchen\" />", html);
        Assert.Contains("<meta property=\"og:description\" content=\"Ein &quot;feiner&quot; Kuchen\" />", html);
        Assert.Contains("<meta property=\"og:url\" content=\"https://foodblog.ch/recipes/7\" />", html);
        Assert.Contains("<meta property=\"og:image\" content=\"https://foodblog.ch/images/7\" />", html);
        Assert.Contains("<meta name=\"twitter:card\" content=\"summary_large_image\" />", html);
        Assert.Contains("<link rel=\"canonical\" href=\"https://foodblog.ch/recipes/7\" />", html);
    }

    [Fact]
    public void Page_with_meta_emits_raw_head_extra_verbatim()
    {
        var meta = new PageMeta { HeadExtra = "<script type=\"application/ld+json\">{}</script>" };

        var html = Layout.Page("Titel", Html.Text("body"), ViewContext.ForGuest(), meta);

        Assert.Contains("<script type=\"application/ld+json\">{}</script>", html);
    }
}
