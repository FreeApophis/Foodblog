using System.Globalization;

namespace Foodblog.Units;

public static class AdminUnitViews
{
    // Full overview page: a single card holding the inline-editable unit rows, styled like the
    // recipe ingredient editor. Rows are added/edited/removed in place via htmx.
    public static string List(IReadOnlyList<Unit> units, ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var head = $"""
            <div class="unit-head">
                <span>{Html.Encode(t("unit.field.name"))}</span>
                <span>{Html.Encode(t("unit.field.abbreviation"))}</span>
                <span>{Html.Encode(t("unit.field.category"))}</span>
                <span>{Html.Encode(t("unit.field.factor"))}</span>
                <span></span>
            </div>
            """;

        var rows = string.Concat(units.Select(u => ViewRow(u, ctx)));

        var section = $"""
            <section id="units-section" class="card unit-editor">
                <div class="card-section-head"><h3>{Html.Encode(t("unit.list_title"))}</h3><span class="rule"></span><span class="count">{units.Count}</span></div>
                <input type="hidden" name="_csrf" id="units-csrf" value="{Html.Encode(csrfToken)}" />
                <div id="units-error" class="unit-error"></div>
                {head}
                <div id="units-list">{rows}</div>
                <button type="button" class="btn-add"
                        hx-get="/admin/units/new-row" hx-target="#units-list" hx-swap="beforeend">
                    {PlusIcon}{Html.Encode(t("unit.new_unit"))}
                </button>
            </section>
            """;

        var breadcrumb = Html.Breadcrumb(
            Crumb.Link(t("nav.admin"), "/admin"),
            Crumb.Current(t("unit.list_title")));

        var body = breadcrumb + Html.Raw(section);
        return Layout.Page(t("unit.list_title"), body, ctx);
    }

    // A read-only row with edit (pencil) and delete (trash) icon buttons.
    public static string ViewRow(Unit unit, ViewContext ctx)
    {
        var (_, t, _) = ctx;
        var id = unit.Id.Value;

        return $"""
            <div class="unit-row" id="unit-row-{id}">
                <span class="unit-cell" data-label="{Html.Encode(t("unit.field.name"))}">{Html.Encode(t(unit.NameKey))}</span>
                <span class="unit-cell" data-label="{Html.Encode(t("unit.field.abbreviation"))}">{Html.Encode(t(unit.AbbreviationKey))}</span>
                <span class="unit-cell" data-label="{Html.Encode(t("unit.field.category"))}">{Html.Encode(t(CategoryKey(unit.Category)))}</span>
                <span class="unit-cell mono" data-label="{Html.Encode(t("unit.field.factor"))}">{Html.Encode(unit.Factor.ToString("G29", CultureInfo.InvariantCulture))}</span>
                <span class="unit-actions">
                    <button type="button" class="icon-btn" title="{Html.Encode(t("common.edit"))}" aria-label="{Html.Encode(t("common.edit"))}"
                            hx-get="/admin/units/{id}/edit-row" hx-target="#unit-row-{id}" hx-swap="outerHTML">{PencilIcon}</button>
                    <button type="button" class="icon-remove" title="{Html.Encode(t("common.delete"))}" aria-label="{Html.Encode(t("common.delete"))}"
                            data-confirm="{Html.Encode(t("common.confirm_delete"))}"
                            hx-post="/admin/units/{id}/delete" hx-target="#unit-row-{id}" hx-swap="outerHTML" hx-include="#units-csrf">{TrashIcon}</button>
                </span>
            </div>
            """;
    }

    public static string EditRow(Unit unit, ViewContext ctx) =>
        EditRow(
            unit.Id,
            ctx.T(unit.NameKey),
            ctx.T(unit.AbbreviationKey),
            ((int)unit.Category).ToString(),
            unit.Factor.ToString("G29", CultureInfo.InvariantCulture),
            ctx);

    // Inline editor for an existing unit; saves to /admin/units/{id}, cancel restores the view row.
    public static string EditRow(UnitId id, string name, string abbreviation, string category, string factor, ViewContext ctx) =>
        EditRowHtml(
            $"unit-row-{id.Value}",
            $"/admin/units/{id.Value}",
            $"/admin/units/{id.Value}/row",
            name,
            abbreviation,
            category,
            factor,
            ctx);

    public static string NewRow(ViewContext ctx) =>
        NewRow(string.Empty, string.Empty, ((int)UnitCategory.Weight).ToString(), "1", ctx);

    // Inline editor for a new unit; saves to /admin/units, cancel removes the row.
    public static string NewRow(string name, string abbreviation, string category, string factor, ViewContext ctx) =>
        EditRowHtml("unit-row-new", "/admin/units", "/admin/units/cancel-new", name, abbreviation, category, factor, ctx);

    // Out-of-band fragment that refreshes the section's error banner (empty list clears it).
    public static string ErrorOob(IReadOnlyList<string> errors, Translate t)
    {
        var content = errors.Count == 0
            ? string.Empty
            : Html.Div("errors", Html.Ul(errors.Select(key => Html.Text(t(key))))).Render();
        return $"""<div id="units-error" class="unit-error" hx-swap-oob="true">{content}</div>""";
    }

    private static string EditRowHtml(
        string rowId,
        string saveUrl,
        string cancelUrl,
        string name,
        string abbreviation,
        string category,
        string factor,
        ViewContext ctx)
    {
        var (_, t, csrfToken) = ctx;

        var categoryOptions = string.Concat(Enum.GetValues<UnitCategory>().Select(c =>
        {
            var selected = ((int)c).ToString() == category ? " selected" : string.Empty;
            return $"""<option value="{(int)c}"{selected}>{Html.Encode(t(CategoryKey(c)))}</option>""";
        }));

        return $"""
            <div class="unit-row unit-row-edit" id="{rowId}">
                <input type="hidden" name="_csrf" value="{Html.Encode(csrfToken)}" />
                <input name="name" value="{Html.Encode(name)}" required placeholder="{Html.Encode(t("unit.field.name"))}" />
                <input name="abbreviation" value="{Html.Encode(abbreviation)}" required placeholder="{Html.Encode(t("unit.field.abbreviation"))}" />
                <select name="category">{categoryOptions}</select>
                <input name="factor" type="number" min="0" step="any" value="{Html.Encode(factor)}" required />
                <span class="unit-actions">
                    <button type="button" class="icon-btn" title="{Html.Encode(t("common.save"))}" aria-label="{Html.Encode(t("common.save"))}"
                            hx-post="{saveUrl}" hx-target="#{rowId}" hx-swap="outerHTML" hx-include="closest .unit-row">{CheckIcon}</button>
                    <button type="button" class="icon-cancel" title="{Html.Encode(t("common.cancel"))}" aria-label="{Html.Encode(t("common.cancel"))}"
                            hx-get="{cancelUrl}" hx-target="#{rowId}" hx-swap="outerHTML">{XIcon}</button>
                </span>
            </div>
            """;
    }

    private static string CategoryKey(UnitCategory category) => category switch
    {
        UnitCategory.Weight => "unit.category.weight",
        UnitCategory.Volume => "unit.category.volume",
        UnitCategory.Piece => "unit.category.piece",
        _ => "unit.category.weight",
    };

    private const string PencilIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>""";

    private const string TrashIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9"><path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2m2 0v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/></svg>""";

    private const string CheckIcon =
        """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>""";

    private const string XIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M18 6 6 18M6 6l12 12"/></svg>""";

    private const string PlusIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14"/></svg>""";
}
