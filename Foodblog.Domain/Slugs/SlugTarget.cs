namespace Foodblog.Domain.Slugs;

// The entity a slug resolves to: a coarse type discriminator plus the integer id within that type.
public sealed record SlugTarget(string EntityType, int EntityId);
