using System.Globalization;

namespace Foodblog.Ingredients;

public static class IngredientViews
{
    // Public ingredient overview: a grid of tiles linking to each detail page.
    public static string Index(PagedResult<Ingredient> page, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        var grid = page.TotalItems == 0
            ? Html.P(Html.Text(t("ingredient.no_ingredients")))
            : Html.Raw($"""<div class="ingredient-grid">{string.Concat(page.Items.Select(i => Tile(i, ctx)))}</div>""");

        var breadcrumb = Html.Breadcrumb(Crumb.Current(t("ingredient.list_title")));
        var pagination = Html.Pagination(page.CurrentPage, page.TotalPages, "/ingredients", t("common.pagination"));

        var body = breadcrumb + Html.H1(t("ingredient.list_title")) + grid + pagination;
        return Layout.Page(t("ingredient.list_title"), body, ctx);
    }

    // Public ingredient detail: name, optional image, description and a nutrition table. Editors get a
    // pencil shortcut into the admin editor.
    public static string Show(Ingredient ingredient, ViewContext ctx)
    {
        var (principal, t, _) = ctx;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("ingredient.list_title"), "/ingredients"),
            Crumb.Current(ingredient.Name.Value));

        var editButton = principal.Can<Edit>(new IngredientResource())
            ? Html.Raw($"""<a class="icon-round" href="/admin/ingredients/{ingredient.Id.Value}/edit" title="{Html.Encode(t("common.edit"))}" aria-label="{Html.Encode(t("common.edit"))}">{PencilIcon}</a>""")
            : HtmlString.Empty;

        var header =
            Html.Raw($"""<div class="ingredient-detail-head"><h1>{Html.Encode(ingredient.Name.Value)}</h1>""") +
            editButton +
            Html.Raw("</div>");

        var media = string.IsNullOrWhiteSpace(ingredient.Image)
            ? HtmlString.Empty
            : Html.Raw($"""<div class="ingredient-detail-media"><img src="{Html.Encode(ingredient.Image)}" alt="{Html.Encode(ingredient.Name.Value)}" /></div>""");

        var description = string.IsNullOrWhiteSpace(ingredient.Description)
            ? HtmlString.Empty
            : Html.Div("post-text", Html.Text(ingredient.Description));

        var body = breadcrumb + header + media + description + Nutrition(ingredient, t);
        return Layout.Page(ingredient.Name.Value, body, ctx);
    }

    private static string Tile(Ingredient ingredient, ViewContext ctx) =>
        $"""<a class="ingredient-tile" href="{ctx.Url(SlugEntityType.Ingredient, ingredient.Id.Value)}">{Html.Encode(ingredient.Name.Value)}</a>""";

    private static HtmlString Nutrition(Ingredient ingredient, Translate t)
    {
        string Row(string labelKey, decimal value, string unit) =>
            $"""<div class="nutrition-row"><span class="nutrition-label">{Html.Encode(t(labelKey))}</span><span class="nutrition-value">{Format(value)} {Html.Encode(unit)}</span></div>""";

        var rows =
            Row("ingredient.field.calorific_value", ingredient.CalorificValue, "kJ/100g") +
            Row("ingredient.field.protein", ingredient.Protein, "g/100g") +
            Row("ingredient.field.fat", ingredient.Fat, "g/100g") +
            Row("ingredient.field.carbohydrates", ingredient.Carbohydrates, "g/100g") +
            Row("ingredient.field.sugar", ingredient.Sugar, "g/100g") +
            Row("ingredient.field.fiber", ingredient.Fiber, "g/100g") +
            Row("ingredient.field.density", ingredient.Density, "g/ml") +
            Row("ingredient.field.piece_count", ingredient.PieceCount, "g");

        return Html.Raw($"""
            <section class="card ingredient-nutrition">
                <div class="card-section-head"><h3>{Html.Encode(t("ingredient.section.nutrition"))}</h3><span class="rule"></span></div>
                <div class="nutrition-grid">{rows}</div>
            </section>
            """);
    }

    private static string Format(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture);

    private const string PencilIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>""";
}
