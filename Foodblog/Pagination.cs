namespace Foodblog;

// Shared paging logic so every paged list (recipes overview, ingredients admin, …) slices the
// same way. The rendering counterpart is Html.Pagination.
public static class Pagination
{
    public const int DefaultPageSize = 12;

    // Reads the requested page from a query parameter, defaulting to 1 when absent or unparseable.
    public static int RequestedPage(Request request, string key = "page")
        => request.Query.GetValueOrNone(key)
            .Select(raw => int.TryParse(raw, out var n) ? n : 1)
            .GetOrElse(1);

    // Slices `all` for the requested page, clamping the page into [1, totalPages] so an out-of-range
    // request lands on the last page rather than an empty one.
    public static PagedResult<T> Paginate<T>(IReadOnlyList<T> all, int requestedPage, int pageSize = DefaultPageSize)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)pageSize));
        var page = Math.Clamp(requestedPage, 1, totalPages);
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>(items, page, totalPages, all.Count);
    }
}
