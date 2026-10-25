namespace Foodblog.Tags;

public static class TagViews
{
    public static string Show(
        Tag tag,
        IReadOnlyList<Recipe> recipes,
        IReadOnlyList<Article> articles,
        IReadOnlyDictionary<UserId, string> authorNames,
        IReadOnlyDictionary<UserId, Option<ImageId>> authorAvatars,
        ViewContext ctx)
    {
        var (_, t, _) = ctx;

        // No tag overview page exists yet, so the "Tag" root still links to the home feed.
        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("tag.title"), "/"),
            Crumb.Current(tag.Name));

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("tag.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(tag.Name)}</h1></div>
            </div>
            """);

        // Recipes reuse the recipe index card; articles reuse the blog feed card.
        HtmlString Section(string titleKey, string gridClass, IEnumerable<string> cards, int count) =>
            count == 0
                ? HtmlString.Empty
                : Html.Raw($"""<h2 class="tag-section-title">{Html.Encode(t(titleKey))}</h2><div class="{gridClass}">{string.Concat(cards)}</div>""");

        var recipeSection = Section(
            "recipe.title",
            "recipe-grid",
            recipes.Select(r => RecipeViews.Card(r, authorNames, authorAvatars, ctx)),
            recipes.Count);

        var articleSection = Section(
            "blog.title",
            "blog-grid",
            articles.Select(a => BlogViews.Card(a, authorNames, authorAvatars, ctx)),
            articles.Count);

        var empty = recipes.Count == 0 && articles.Count == 0
            ? Html.P(Html.Text(t("tag.no_items")))
            : HtmlString.Empty;

        var body = breadcrumb + head + recipeSection + articleSection + empty;

        return Layout.Page(tag.Name, body, ctx);
    }
}
