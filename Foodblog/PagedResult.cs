namespace Foodblog;

// One page of a larger list: the items to show, which page this is, and how many pages exist in
// total. Produced by Pagination.Paginate and consumed by views (plus Html.Pagination for the nav).
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int CurrentPage, int TotalPages, int TotalItems);
