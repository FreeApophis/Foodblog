namespace Foodblog.Application.Search;

public sealed record SearchResult(
    string Type,
    int Id,
    string Title,
    string Snippet,
    float Score);
