namespace Foodblog.Domain.Tags;

// A tag identified by its integer key plus display name. Used to register tag slugs in the central
// slug registry, where the URL slug lives (the tags table itself no longer stores a slug).
public sealed record TagEntry(int Id, string Name);
