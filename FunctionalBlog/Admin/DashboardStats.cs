namespace FunctionalBlog.Admin;

// Aggregate content counts shown on the /admin overview. IncompleteIngredients drives the
// "needs attention" notice (ingredients still missing nutrition/other data); SiteUrlMissing flags
// an unset/invalid site.url, without which reset and verification mails cannot be sent.
public sealed record DashboardStats(
    int Articles,
    int Recipes,
    int Ingredients,
    int Pages,
    int Images,
    int Users,
    int IncompleteIngredients,
    bool SiteUrlMissing = false);
