namespace Foodblog;

public sealed record ViewContext(IPrincipal Principal, Translate T, string CsrfToken, string Theme = "light", string Language = Languages.Default, SlugIndex? Slugs = null, string SiteName = "foodblog.ch", FocalIndex? Focal = null)
{
    public static ViewContext ForGuest(string csrfToken = "") =>
        new(Guest.Instance, Funcky.Functional.Identity, csrfToken);

    // The ` style="object-position:.."` attribute (with a leading space) for an image referenced by
    // a "/images/{id}" URL, or an empty string when the image is centred/unknown. Designed to be
    // dropped straight into an <img> tag next to a cover-cropping rule so the subject stays in frame.
    public string FocalStyle(string imageUrl) =>
        ImageIdFrom(imageUrl)
            .SelectMany(id => (Focal ?? FocalIndex.Empty).PositionFor(id))
            .Match(none: string.Empty, some: position => $" style=\"object-position:{position}\"");

    // The canonical path for an entity (e.g. "/recipes/ruehrkuchen"), falling back to the numeric
    // id when no slug is registered or no slug index was threaded in (guest/test contexts).
    public string Url(string entityType, int id) => (Slugs ?? SlugIndex.Empty).Url(entityType, id);

    // The bare slug segment for an entity (numeric id when unregistered) — for callers that build
    // their own URL around it, such as SEO canonical links.
    public string SlugFor(string entityType, int id) => (Slugs ?? SlugIndex.Empty).For(entityType, id);

    // The canonical (slug) detail URL for an entity whose id-based action path is given, e.g. turning
    // an edit form's "/recipes/42" POST target into "/recipes/ruehrkuchen". Falls back to the action
    // path unchanged when it has no trailing numeric id (so non-entity actions stay intact).
    public string CanonicalFromAction(string entityType, string actionPath)
    {
        var lastSlash = actionPath.LastIndexOf('/');
        return lastSlash >= 0 && int.TryParse(actionPath[(lastSlash + 1)..], out var id)
            ? Url(entityType, id)
            : actionPath;
    }

    // The canonical /tag/{slug} path for a tag referenced by name.
    public string TagUrl(string name) => (Slugs ?? SlugIndex.Empty).TagUrl(name);

    // Most views only need these three; keep the 3-arg deconstruction working alongside the
    // record's generated 4-arg one so they don't all have to spell out Theme.
    public void Deconstruct(out IPrincipal principal, out Translate t, out string csrfToken)
    {
        principal = Principal;
        t = T;
        csrfToken = CsrfToken;
    }

    // Parses the trailing numeric id out of a "/images/{id}" URL. None for anything else.
    private static Option<int> ImageIdFrom(string imageUrl)
    {
        const string prefix = "/images/";
        return imageUrl.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(imageUrl[prefix.Length..], out var id)
            ? Option.Some(id)
            : Option<int>.None;
    }
}
