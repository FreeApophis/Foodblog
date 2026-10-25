namespace Foodblog.Application.Search;

// A snapshot of what the full-text index currently holds, surfaced on the admin maintenance page.
// LastRebuilt is None until the index has been (re)built at least once in this process.
public sealed record SearchIndexStatus(
    Option<DateTimeOffset> LastRebuilt,
    int Articles,
    int Recipes,
    int Ingredients,
    int Pages)
{
    public int Total => Articles + Recipes + Ingredients + Pages;
}
