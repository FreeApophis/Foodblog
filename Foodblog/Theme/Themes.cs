namespace Foodblog.Theme;

public static class Themes
{
    public const string Default = "light";

    public static readonly IReadOnlyList<string> Supported = ["light", "dark"];

    public static string Normalize(string theme) =>
        Supported.Contains(theme) ? theme : Default;
}
