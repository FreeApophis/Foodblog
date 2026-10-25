namespace Foodblog.Admin;

public static class AdminDashboardViews
{
    private const string UsersIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>""";

    private const string RolesIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z"/><path d="m9 12 2 2 4-4"/></svg>""";

    private const string UnitsIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18M3 12h18M3 18h18"/><path d="M7 3v3M12 9v3M17 15v3"/></svg>""";

    private const string IngredientsIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2a7 7 0 0 0-7 7c0 3 2 5 2 8a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2c0-3 2-5 2-8a7 7 0 0 0-7-7Z"/><path d="M9 21h6"/></svg>""";

    private const string ImagesIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="9" cy="9" r="2"/><path d="m21 15-3.5-3.5L8 21"/></svg>""";

    private const string PagesIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z"/><path d="M14 2v6h6M9 13h6M9 17h6"/></svg>""";

    private const string BlogIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2Z"/></svg>""";

    private const string TranslationsIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="m5 8 6 6M4 14l6-6 2-3M2 5h12M7 2h1M22 22l-5-10-5 10M14 18h6"/></svg>""";

    private const string SearchIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></svg>""";

    private const string CleanupIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6M10 11v6M14 11v6M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>""";

    private const string SettingsIcon =
        """<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1Z"/></svg>""";

    private const string WarnIcon =
        """<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z"/><path d="M12 9v4M12 17h.01"/></svg>""";

    private static readonly IReadOnlyList<CardDef> AllCards =
    [
        new("/admin/users", "admin.dashboard.card.users", "admin.dashboard.card.users_desc", UsersIcon, p => p.Can<Manage>(new UserResource())),
        new("/admin/roles", "admin.dashboard.card.roles", "admin.dashboard.card.roles_desc", RolesIcon, p => p.Can<Manage>(new RoleResource())),
        new("/admin/units", "admin.dashboard.card.units", "admin.dashboard.card.units_desc", UnitsIcon, p => p.Can<Manage>(new UnitResource())),
        new("/admin/ingredients", "admin.dashboard.card.ingredients", "admin.dashboard.card.ingredients_desc", IngredientsIcon, p => p.Can<Manage>(new IngredientResource())),
        new("/admin/search", "admin.dashboard.card.search", "admin.dashboard.card.search_desc", SearchIcon, p => p.Can<Manage>(new SearchResource())),
        new("/images", "admin.dashboard.card.images", "admin.dashboard.card.images_desc", ImagesIcon, p => p.Can<Manage>(new ImageResource())),
        new("/admin/images/cleanup", "admin.dashboard.card.image_cleanup", "admin.dashboard.card.image_cleanup_desc", CleanupIcon, p => p.Can<Manage>(new ImageResource())),
        new("/pages", "admin.dashboard.card.pages", "admin.dashboard.card.pages_desc", PagesIcon, p => p.Can<Create>(new PageResource())),
        new("/", "admin.dashboard.card.blog", "admin.dashboard.card.blog_desc", BlogIcon, p => p.Can<Create>(new ArticleResource())),
        new("/admin/translations", "admin.dashboard.card.translations", "admin.dashboard.card.translations_desc", TranslationsIcon, p => p.Can<Manage>(new UserResource())),
        new("/admin/settings", "admin.dashboard.card.settings", "admin.dashboard.card.settings_desc", SettingsIcon, p => p.Can<Manage>(new SettingsResource())),
    ];

    // The admin landing page: a grid of cards, one per manageable section. Each card is only
    // rendered when the current principal actually has permission to reach its target route,
    // so the dashboard never advertises a section the user would get a 403 on.
    public static string Dashboard(DashboardStats stats, ViewContext ctx)
    {
        var (principal, t, _) = ctx;

        var visible = AllCards.Where(c => c.IsVisible(principal)).ToList();

        var grid = visible.Count > 0
            ? Html.Raw($"""<div class="admin-grid">{string.Concat(visible.Select(c => Card(c, t)))}</div>""")
            : Html.P(Html.Text(t("admin.dashboard.empty")));

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("admin.dashboard.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(t("admin.dashboard.title"))}</h1></div>
                <p class="page-head-blurb">{Html.Encode(t("admin.dashboard.blurb"))}</p>
            </div>
            """);

        return Layout.Page(t("admin.dashboard.title"), head + StatsStrip(stats, principal, t) + SiteUrlWarning(stats, principal, t) + Attention(stats, principal, t) + grid, ctx);
    }

    // True when the principal can reach at least one admin section — used by the nav to decide
    // whether to surface the "Admin" link at all.
    public static bool HasAnyAccess(IPrincipal principal) =>
        AllCards.Any(c => c.IsVisible(principal));

    // A row of count tiles, one per content area the principal can reach. Counts come from the
    // handler; visibility mirrors the section cards so the strip never reveals an off-limits area.
    private static HtmlString StatsStrip(DashboardStats stats, IPrincipal principal, Translate t)
    {
        var tiles = AllStats.Where(s => s.IsVisible(principal)).ToList();
        if (tiles.Count == 0)
        {
            return Html.Raw(string.Empty);
        }

        return Html.Raw($"""<div class="stat-strip">{string.Concat(tiles.Select(s => StatTile(s, stats, t)))}</div>""");
    }

    private static HtmlString StatTile(StatDef def, DashboardStats stats, Translate t) =>
        Html.Raw($"""
            <a class="stat-tile" href="{Html.Encode(def.Href)}">
                <span class="stat-num">{def.Count(stats)}</span>
                <span class="stat-label">{Html.Encode(t(def.LabelKey))}</span>
            </a>
            """);

    // Shown to settings managers while site.url is unset/invalid: password-reset and verification
    // mails are not sent until it is fixed.
    private static HtmlString SiteUrlWarning(DashboardStats stats, IPrincipal principal, Translate t)
    {
        if (!stats.SiteUrlMissing || !principal.Can<Manage>(new SettingsResource()))
        {
            return Html.Raw(string.Empty);
        }

        return Html.Raw($"""
            <a class="attention" href="/admin/settings">
                <span class="warn-badge">{WarnIcon}</span>
                {Html.Encode(t("admin.dashboard.attention.site_url_missing"))}
            </a>
            """);
    }

    // A "needs attention" notice for ingredients still missing information, shown only to users who
    // can act on it (manage ingredients) and only when there is something to fix.
    private static HtmlString Attention(DashboardStats stats, IPrincipal principal, Translate t)
    {
        if (stats.IncompleteIngredients <= 0 || !principal.Can<Manage>(new IngredientResource()))
        {
            return Html.Raw(string.Empty);
        }

        return Html.Raw($"""
            <a class="attention" href="/admin/ingredients">
                <span class="warn-badge">{WarnIcon}</span>
                {Html.Encode(t("admin.dashboard.attention.incomplete_ingredients"))} ({stats.IncompleteIngredients})
            </a>
            """);
    }

    private static HtmlString Card(CardDef def, Translate t) =>
        Html.Raw($"""
            <a class="admin-card" href="{Html.Encode(def.Href)}">
                <span class="admin-card-icon">{def.Icon}</span>
                <span class="admin-card-body">
                    <span class="admin-card-title">{Html.Encode(t(def.TitleKey))}</span>
                    <span class="admin-card-desc">{Html.Encode(t(def.DescKey))}</span>
                </span>
            </a>
            """);

    private sealed record CardDef(string Href, string TitleKey, string DescKey, string Icon, Func<IPrincipal, bool> IsVisible);

    private static readonly IReadOnlyList<StatDef> AllStats =
    [
        new("/", "admin.dashboard.stat.articles", s => s.Articles, p => p.Can<Create>(new ArticleResource())),
        new("/recipes", "admin.dashboard.stat.recipes", s => s.Recipes, p => p.Can<Create>(new RecipeResource())),
        new("/admin/ingredients", "admin.dashboard.stat.ingredients", s => s.Ingredients, p => p.Can<Manage>(new IngredientResource())),
        new("/pages", "admin.dashboard.stat.pages", s => s.Pages, p => p.Can<Create>(new PageResource())),
        new("/images", "admin.dashboard.stat.images", s => s.Images, p => p.Can<Manage>(new ImageResource())),
        new("/admin/users", "admin.dashboard.stat.users", s => s.Users, p => p.Can<Manage>(new UserResource())),
    ];

    private sealed record StatDef(string Href, string LabelKey, Func<DashboardStats, int> Count, Func<IPrincipal, bool> IsVisible);
}
