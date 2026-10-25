namespace Foodblog.Application.Tags;

public interface ITagRepository
{
    // Looks up a single tag by its URL slug (resolved through the central slug registry, e.g.
    // "suess"). None when no tag matches.
    ValueTask<Option<Tag>> FindBySlug(string slug);

    // Every tag as an (id, name) pair — for registering tag slugs in the central registry at startup.
    ValueTask<IReadOnlyList<TagEntry>> All();

    // The id of the tag with this exact name (case-insensitive), for ensuring its slug after a save.
    ValueTask<Option<int>> FindIdByName(string name);
}
