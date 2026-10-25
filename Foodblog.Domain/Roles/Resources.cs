namespace Foodblog.Domain.Roles;

// The canonical set of permissionable resources — the single source of truth for the role editor's
// resource dropdown, so it can never drift from the resources the app actually guards (the previous
// hard-coded list had grown stale). Add a new IResource here too; ResourcesTests enforces
// completeness via reflection so an omission fails the build.
public static class Resources
{
    public static IReadOnlyList<IResource> All { get; } =
    [
        new ArticleResource(),
        new ImageResource(),
        new IngredientResource(),
        new PageResource(),
        new RecipeResource(),
        new RoleResource(),
        new RuleResource(),
        new SearchResource(),
        new SettingsResource(),
        new UnitResource(),
        new UserResource(),
    ];

    // Resource keys in stable alphabetical order, for the role editor's resource dropdown and any
    // other place that needs to list every guardable resource.
    public static IReadOnlyList<string> AllKeys { get; } =
        All.Select(resource => resource.ResourceKey).OrderBy(Functional.Identity, StringComparer.Ordinal).ToList();
}
