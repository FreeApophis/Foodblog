namespace FunctionalBlog;

public sealed record Env(
    IArticleRepository Articles,
    IUserRepository Users,
    IRoleRepository Roles,
    ISessionStore Sessions,
    IPasswordResetTokenStore PasswordResets,
    IPasswordHasher PasswordHasher,
    IClock Clock,
    ILog Log,
    IPrincipal CurrentUser,
    IRecipeRepository Recipes,
    IIngredientRepository Ingredients,
    IUnitRepository Units,
    IImageRepository Images,
    IPageRepository Pages,
    ITranslationRepository? Translations = null,
    TranslationCache? TranslationCache = null,
    ISearchIndex? Search = null,
    IQuickSearch? QuickSearch = null,
    ITagRepository? Tags = null,
    ISlugRepository? Slugs = null,
    SlugIndex? SlugIndex = null,
    FocalIndex? Focal = null,
    IConfigurationRepository? Configuration = null,
    ConfigurationCache? Config = null,
    IEmailSender? Email = null,
    IEmailVerificationTokenStore? EmailVerifications = null,
    string Language = Languages.Default,
    string Theme = "light",
    string CsrfToken = "")
{
    public Translate T =>
        key => TranslationCache?.Get(key, Language) ?? key;

    // The configured site name, falling back to the historical brand for minimal/test envs.
    public string SiteName => Config?.SiteName ?? "foodblog.ch";

    // Convenience for the write path: a SlugService over the wired repository, or null when no
    // slug repository is configured (e.g. minimal test envs).
    public SlugService? SlugMaker => Slugs is null ? null : new SlugService(Slugs);

    // Registers (or refreshes) an entity's slug and returns it. Falls back to the numeric id when
    // no slug repository is configured, so id-based redirects still work in minimal test envs.
    public async ValueTask<string> EnsureSlug(string entityType, int entityId, string sourceText) =>
        SlugMaker is { } maker
            ? await maker.Ensure(entityType, entityId, sourceText)
            : entityId.ToString();

    public ViewContext Ctx => new(CurrentUser, T, CsrfToken, Theme, Language, SlugIndex, SiteName, Focal);
}
