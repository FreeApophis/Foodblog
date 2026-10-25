namespace Foodblog.Test;

public sealed class BreadcrumbTests
{
    [Fact]
    public void Renders_link_crumbs_as_anchors_and_the_current_crumb_as_accented_text()
    {
        var html = Html.Breadcrumb(
            Crumb.Link("Rezepte", "/recipes"),
            Crumb.Current("Risotto")).Render();

        Assert.Contains("""<nav class="breadcrumb" """, html);
        Assert.Contains("""<a href="/recipes">Rezepte</a>""", html);
        Assert.Contains("""<span class="crumb-current" aria-current="page">Risotto</span>""", html);
        Assert.Contains("crumb-sep", html);
    }

    [Fact]
    public void Puts_a_separator_between_every_crumb_but_not_at_the_ends()
    {
        var html = Html.Breadcrumb(
            Crumb.Link("Rezepte", "/recipes"),
            Crumb.Link("Älpler One-Pot", "/recipes/1"),
            Crumb.Current("Bearbeiten")).Render();

        Assert.Equal(2, CountOccurrences(html, "crumb-sep"));
    }

    [Fact]
    public void Encodes_labels_to_prevent_injection()
    {
        var html = Html.Breadcrumb(Crumb.Current("<b>boom</b>")).Render();

        Assert.DoesNotContain("<b>boom</b>", html);
        Assert.Contains("&lt;b&gt;boom&lt;/b&gt;", html);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
