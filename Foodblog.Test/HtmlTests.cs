namespace Foodblog.Test;

public sealed class HtmlTests
{
    [Fact]
    public void H2_passes_html_content_through_unescaped()
    {
        var link = Html.Link("/articles/2", "Macarons selbst backen");

        var result = Html.H2(link);

        Assert.Equal("""<h2><a href="/articles/2">Macarons selbst backen</a></h2>""", result.Render());
    }

    [Fact]
    public void Text_encodes_html_special_chars()
    {
        var result = Html.Text("<script>alert('xss')</script>");

        Assert.Equal("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", result.Render());
    }

    [Fact]
    public void Raw_passes_html_through_unencoded()
    {
        var result = Html.Raw("<b>bold</b>");

        Assert.Equal("<b>bold</b>", result.Render());
    }

    [Fact]
    public void HtmlString_concatenation_renders_both_sides()
    {
        var result = Html.Text("Hallo ") + Html.Raw("<b>Welt</b>");

        Assert.Equal("Hallo <b>Welt</b>", result.Render());
    }

    [Fact]
    public void H1_encodes_the_title()
    {
        var result = Html.H1("<script>");

        Assert.Equal("<h1>&lt;script&gt;</h1>", result.Render());
    }

    [Fact]
    public void P_passes_html_body_through()
    {
        var result = Html.P(Html.Link("/", "Home"));

        Assert.Equal("""<p><a href="/">Home</a></p>""", result.Render());
    }
}
