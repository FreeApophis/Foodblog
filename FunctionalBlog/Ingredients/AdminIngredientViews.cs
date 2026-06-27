namespace FunctionalBlog.Ingredients;

public static class AdminIngredientViews
{
    // Paginated overview: a card of read-only ingredient rows. Editing happens on a dedicated page
    // (the pencil link) rather than inline, because an ingredient carries too many fields for one
    // row. Rows that are still missing information are flagged with a yellow warning badge.
    public static string List(PagedResult<Ingredient> page, ViewContext ctx, string error = "")
    {
        var (_, t, csrfToken) = ctx;

        var head = $"""
            <div class="ingredient-head">
                <span>{Html.Encode(t("ingredient.field.name"))}</span>
                <span>{Html.Encode(t("ingredient.field.description"))}</span>
                <span></span>
            </div>
            """;

        var rowsHtml = page.TotalItems == 0
            ? $"""<p class="admin-empty">{Html.Encode(t("ingredient.no_ingredients"))}</p>"""
            : string.Concat(page.Items.Select(i => Row(i, ctx)));

        var errorHtml = error == "in-use"
            ? Html.Div("errors", Html.P(Html.Text(t("ingredient.error.in_use")))).Render()
            : string.Empty;

        var section = $"""
            <section id="ingredients-section" class="card ingredient-editor">
                <div class="card-section-head"><h3>{Html.Encode(t("ingredient.list_title"))}</h3><span class="rule"></span><span class="count">{page.TotalItems}</span></div>
                {errorHtml}
                {head}
                <div id="ingredients-list">{rowsHtml}</div>
                <a class="btn-add" href="/admin/ingredients/new">{PlusIcon}{Html.Encode(t("ingredient.new_ingredient"))}</a>
            </section>
            """;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("ingredient.list_title")));

        var pagination = Html.Pagination(page.CurrentPage, page.TotalPages, "/admin/ingredients", t("common.pagination"));

        var body = breadcrumb + Html.Raw(section) + pagination;
        return Layout.Page(t("ingredient.list_title"), body, ctx);
    }

    // Dedicated full-page editor for one ingredient (new or existing). Description is a multiline
    // textarea; the numeric nutrition fields share a responsive grid.
    public static string Form(
        IReadOnlyList<string> errors,
        string name,
        string description,
        string image,
        string density,
        string pieceCount,
        string calorificValue,
        string protein,
        string fat,
        string carbohydrates,
        string sugar,
        string fiber,
        ViewContext ctx,
        string formAction = "/admin/ingredients",
        string titleKey = "ingredient.new_title")
    {
        var (_, t, csrfToken) = ctx;

        var errorHtml = errors.Count == 0
            ? HtmlString.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key)))));

        // A labelled number input with the unit shown as an addon inside the box (right side), per
        // the recipe-edit design. Short labels keep every box on the same baseline.
        static HtmlString NumberField(string labelKey, string fieldName, string value, string unit, Translate translate, string step = "any", string min = "0")
        {
            var group = $"""
                <div class="input-unit">
                    <input name="{fieldName}" type="number" min="{min}" step="{step}" value="{Html.Encode(value)}" />
                    <span class="unit-addon">{Html.Encode(unit)}</span>
                </div>
                """;
            return Html.Label(Html.Text(translate(labelKey)) + Html.Raw(group));
        }

        var basics =
            Html.Raw("""<section class="card">""") +
            Html.Label(Html.Text(t("ingredient.field.name")) + Html.Input("name", name)) +
            Html.Label(Html.Text(t("ingredient.field.description")) + Html.Raw($"""<textarea name="description" rows="4">{Html.Encode(description)}</textarea>""")) +
            Html.Label(Html.Text(t("ingredient.field.image")) + Html.Input("image", image)) +
            Html.Raw("</section>");

        var nutrition =
            Html.Raw($"""<section class="card">{SectionHead(t("ingredient.section.nutrition"))}<div class="ingredient-edit-grid">""") +
            NumberField("ingredient.field.density", "density", density, "g/ml", t, step: "0.001", min: "0.001") +
            NumberField("ingredient.field.piece_count", "piece_count", pieceCount, "g", t) +
            NumberField("ingredient.field.calorific_value", "calorific_value", calorificValue, "kJ/100g", t) +
            NumberField("ingredient.field.protein", "protein", protein, "g/100g", t) +
            NumberField("ingredient.field.fat", "fat", fat, "g/100g", t) +
            NumberField("ingredient.field.carbohydrates", "carbohydrates", carbohydrates, "g/100g", t) +
            NumberField("ingredient.field.sugar", "sugar", sugar, "g/100g", t) +
            NumberField("ingredient.field.fiber", "fiber", fiber, "g/100g", t) +
            Html.Raw("</div></section>");

        var saveBar =
            Html.Raw("""<div class="save-bar">""") +
            Html.Button(t("ingredient.submit")) +
            Html.Raw($"""<a class="btn btn-secondary" href="/admin/ingredients">{Html.Encode(t("common.cancel"))}</a>""") +
            Html.Raw("</div>");

        var form = Html.Form(formAction, Html.CsrfField(csrfToken) + basics + nutrition + saveBar, cssClass: "ingredient-form");

        // Edit gains a crumb for the specific ingredient (linking to its public page), matching the
        // recipe/page/article edit breadcrumbs; new has no entity to link to yet.
        var breadcrumb = titleKey == "ingredient.edit_title"
            ? Html.Breadcrumb(
                Crumb.Link(t("nav.admin"), "/admin"),
                Crumb.Link(t("ingredient.list_title"), "/admin/ingredients"),
                Crumb.Link(name, ctx.CanonicalFromAction(SlugEntityType.Ingredient, formAction)),
                Crumb.Current(t("common.edit")))
            : Html.Breadcrumb(
                Crumb.Link(t("nav.admin"), "/admin"),
                Crumb.Link(t("ingredient.list_title"), "/admin/ingredients"),
                Crumb.Current(t("common.new")));

        var body = breadcrumb +
            Html.H1(t(titleKey)) +
            errorHtml +
            form;

        return Layout.Page(t(titleKey), body, ctx);
    }

    private static string Row(Ingredient ingredient, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;
        var id = ingredient.Id.Value;

        var warning = ingredient.HasMissingInformation
            ? $"""<span class="warn-badge" title="{Html.Encode(t("ingredient.incomplete"))}" aria-label="{Html.Encode(t("ingredient.incomplete"))}">{WarnIcon}</span>"""
            : string.Empty;

        var deleteButton = Html.Raw($"""<button type="submit" class="icon-remove" title="{Html.Encode(t("common.delete"))}" aria-label="{Html.Encode(t("common.delete"))}">{TrashIcon}</button>""");
        var deleteForm = Html.Form(
            $"/admin/ingredients/{id}/delete",
            Html.CsrfField(csrfToken) + deleteButton,
            cssClass: "inline-form",
            confirm: t("common.confirm_delete")).Render();

        return $"""
            <div class="ingredient-row" id="ingredient-row-{id}">
                <span class="ingredient-cell" data-label="{Html.Encode(t("ingredient.field.name"))}">{warning}<a href="{Html.Encode(ctx.Url(SlugEntityType.Ingredient, id))}">{Html.Encode(ingredient.Name.Value)}</a></span>
                <span class="ingredient-cell" data-label="{Html.Encode(t("ingredient.field.description"))}">{Html.Encode(ingredient.Description)}</span>
                <span class="unit-actions">
                    <a class="icon-btn" href="/admin/ingredients/{id}/edit" title="{Html.Encode(t("common.edit"))}" aria-label="{Html.Encode(t("common.edit"))}">{PencilIcon}</a>
                    {deleteForm}
                </span>
            </div>
            """;
    }

    // A card section header: serif title + a thin divider rule, matching the recipe form.
    private static string SectionHead(string title) =>
        $"""<div class="card-section-head"><h3>{Html.Encode(title)}</h3><span class="rule"></span></div>""";

    private const string PencilIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>""";

    private const string TrashIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9"><path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2m2 0v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/></svg>""";

    private const string PlusIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14"/></svg>""";

    // Yellow warning triangle shown beside ingredients that still need information.
    private const string WarnIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.3 3.8 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.8a2 2 0 0 0-3.4 0Z"/><path d="M12 9v4M12 17h.01"/></svg>""";
}
