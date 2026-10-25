namespace Foodblog.Translations;

public static class Languages
{
    public const string De = "de";
    public const string En = "en";
    public const string It = "it";
    public const string Fr = "fr";
    public const string Default = De;

    public static readonly string[] Supported = [De, En, It, Fr];

    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        [De] = "Deutsch",
        [En] = "English",
        [It] = "Italiano",
        [Fr] = "Français",
    };
}
