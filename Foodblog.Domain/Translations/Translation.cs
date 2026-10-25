namespace Foodblog.Domain.Translations;

public sealed record Translation(string Key, string Language, string? Variant, string Text);
