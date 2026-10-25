namespace FunctionalBlog;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var web = builder.Build();

        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "./data";
        var dbPath = Path.Combine(dataDir, "database", "blog.db");
        var indexPath = Path.Combine(dataDir, "full-text-search");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        Directory.CreateDirectory(Path.Combine(dataDir, "config"));

        var connectionString = $"Data Source={dbPath}";

        DatabaseMigrator.Migrate(connectionString);
        var connections = SqliteConnectionFactory.Create(connectionString);

        var translations = new SqliteTranslationRepository(connections);
        var env = BuildEnv(connections, translations);

        await Seeder.SeedAsync(env, Option.FromNullable(Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_EMAIL")));
        await SlugBackfill.Run(new SlugService(env.Slugs!), env.Articles, env.Recipes, env.Pages, env.Ingredients, env.Tags!);
        var translationCache = await TranslationCache.LoadAsync(translations);
        var configurationCache = await ConfigurationCache.LoadAsync(env.Configuration!);
        var email = new SmtpEmailSender(configurationCache, env.Log);
        env = env with { TranslationCache = translationCache, Config = configurationCache, Email = email };

        using var search = new Search.LeanCorpusSearchIndex(indexPath);
        await search.RebuildAsync(env.Articles, env.Recipes, env.Ingredients, env.Pages);
        env = env with { Search = search };

        App app = Functional.Compose(
            _ => env => ValueTask.FromResult(Response.NotFound(env.Ctx)),
            ContentSecurityPolicy.Create(),
            LanguageMiddleware.Create(),
            ThemeMiddleware.Create(),
            Middlewares.Recover,
            Middlewares.RequestLogging,
            AuthMiddleware.Create(),
            CsrfMiddleware.Create(),
            SlugMiddleware.Create(),
            FocalMiddleware.Create(),
            Router.Create(Routes.Build()));

        web.UseForwardedHeaders(ReverseProxy.Options());

        web.Run(async http =>
        {
            var request = await HttpAdapter.ToRequest(http);
            var response = await app(request)(env);
            await HttpAdapter.WriteResponse(http, response);
        });

        web.Run();
    }

    private static Env BuildEnv(SqliteConnectionFactory connections, ITranslationRepository translations)
        => new(
            Articles: new SqliteArticleRepository(connections),
            Users: new SqliteUserRepository(connections),
            Roles: new SqliteRoleRepository(connections),
            Sessions: new SqliteSessionStore(connections),
            PasswordResets: new SqlitePasswordResetTokenStore(connections),
            PasswordHasher: new Pbkdf2PasswordHasher(),
            Clock: new SystemClock(),
            Log: new ConsoleLog(),
            CurrentUser: Guest.Instance,
            Recipes: new SqliteRecipeRepository(connections),
            Ingredients: new SqliteIngredientRepository(connections),
            Units: new SqliteUnitRepository(connections),
            Images: new SqliteImageRepository(connections),
            Pages: new SqlitePageRepository(connections),
            QuickSearch: new SqliteQuickSearch(connections),
            Tags: new SqliteTagRepository(connections),
            Slugs: new SqliteSlugRepository(connections),
            Configuration: new SqliteConfigurationRepository(connections),
            EmailVerifications: new SqliteEmailVerificationTokenStore(connections),
            Translations: translations);
}
