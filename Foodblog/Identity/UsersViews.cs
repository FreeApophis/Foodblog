namespace Foodblog.Identity;

public static class UsersViews
{
    public static string Index(IReadOnlyList<(User User, int RecipeCount)> entries, ViewContext ctx)
    {
        var (_, t, _) = ctx;

        string Card((User User, int RecipeCount) e)
        {
            var name = e.User.DisplayName.Value;
            var initial = name.Length > 0 ? name[..1].ToUpperInvariant() : "?";
            var year = e.User.CreatedAt.Year;
            var isAuthor = e.RecipeCount > 0;

            var hue = Hue(name);
            var avatarStyle = isAuthor
                ? $"background:linear-gradient(150deg, oklch(0.74 0.06 {hue}), oklch(0.66 0.07 {(hue + 40) % 360}));"
                : string.Empty;
            var initialStyle = isAuthor ? "color:rgba(255,255,255,0.92);" : string.Empty;
            var badge = isAuthor ? $"""<span class="user-badge">{Html.Encode(t("users.author_badge"))}</span>""" : string.Empty;

            var avatarInner = e.User.AvatarImageId is [var avatarId]
                ? $"""<img class="user-avatar-photo" src="/images/{avatarId.Value}" alt="{Html.Encode(name)}" />"""
                : $"""<span class="initial" style="{initialStyle}">{Html.Encode(initial)}</span>""";
            var avatarBg = e.User.AvatarImageId is [_] ? string.Empty : avatarStyle;

            var label = e.RecipeCount == 1
                ? $"1 {t("users.recipe_singular")}"
                : $"{e.RecipeCount} {t("users.recipe_plural")}";

            return $"""
                <article class="user-card">
                    <div class="user-avatar" style="{avatarBg}">{avatarInner}{badge}</div>
                    <div class="user-card-body">
                        <h3>{Html.Encode(name)}</h3>
                        <p class="user-joined">{Html.Encode(t("users.joined"))} {year}</p>
                    </div>
                    <div class="user-card-foot{(isAuthor ? " is-author" : string.Empty)}">{UtensilsIcon}<span class="user-recipes">{Html.Encode(label)}</span></div>
                </article>
                """;
        }

        var grid = entries.Count > 0
            ? Html.Raw($"""<div class="user-grid">{string.Concat(entries.Select(Card))}</div>""")
            : Html.P(Html.Text(t("users.empty")));

        var head = Html.Raw($"""
            <div class="page-head">
                <div class="eyebrow eyebrow-accent">{Html.Encode(t("users.eyebrow"))}</div>
                <div class="page-head-row"><h1>{Html.Encode(t("users.title"))}</h1></div>
                <p class="page-head-blurb">{Html.Encode(t("users.blurb"))}</p>
            </div>
            """);

        return Layout.Page(t("users.title"), head + grid, ctx);
    }

    // Fork + spoon, inherits the footer's text colour (green for authors, muted otherwise).
    private const string UtensilsIcon =
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 2v7c0 1.1.9 2 2 2h0a2 2 0 0 0 2-2V2M5 2v20M16 2c-1.7 0-3 2-3 5s1.3 5 3 5v10"/></svg>""";

    // Deterministic hue (0–359) from the display name, matching the design's avatar tint.
    private static int Hue(string name)
    {
        var h = 0;
        foreach (var c in name)
        {
            h = ((h * 31) + c) % 360;
        }

        return h;
    }
}
