namespace Foodblog;

// One segment of a breadcrumb trail. A crumb with an Href renders as a link; a crumb without one
// (the current page) renders as plain, accented text. Trails start at level 1 — there is no "home".
public readonly record struct Crumb(string Label, string? Href = null)
{
    public static Crumb Link(string label, string href) => new(label, href);

    public static Crumb Current(string label) => new(label, null);
}
