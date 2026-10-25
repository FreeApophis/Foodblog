namespace Foodblog;

// Optional per-page metadata for the document head: Open Graph / Twitter share cards,
// the canonical URL, and a raw HeadExtra slot for structured data (e.g. JSON-LD). All
// fields are optional; empty ones are simply not rendered. HeadExtra is emitted verbatim,
// so its producer is responsible for escaping it safely.
public sealed record PageMeta
{
    public string Description { get; init; } = string.Empty;

    public string ImageUrl { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Type { get; init; } = "website";

    public string HeadExtra { get; init; } = string.Empty;
}
