namespace Foodblog.Domain.Tags;

// A tag as stored in the normalized dictionary: a case-folded lookup Slug plus the
// display Name. Spans articles and recipes (linked through taggables/taggings).
public sealed record Tag(string Slug, string Name);
