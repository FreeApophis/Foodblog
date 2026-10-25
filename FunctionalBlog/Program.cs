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
        using var connection = SqliteConnectionFactory.Create(connectionString);

        var translations = new SqliteTranslationRepository(connection);
        var env = BuildEnv(connection, translations);

        await Seeder.SeedAsync(env);
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

    private static Env BuildEnv(System.Data.IDbConnection connection, ITranslationRepository translations)
        => new(
            Articles: new SqliteArticleRepository(connection),
            Users: new SqliteUserRepository(connection),
            Roles: new SqliteRoleRepository(connection),
            Sessions: new SqliteSessionStore(connection),
            PasswordResets: new SqlitePasswordResetTokenStore(connection),
            PasswordHasher: new Pbkdf2PasswordHasher(),
            Clock: new SystemClock(),
            Log: new ConsoleLog(),
            CurrentUser: Guest.Instance,
            Recipes: new SqliteRecipeRepository(connection),
            Ingredients: new SqliteIngredientRepository(connection),
            Units: new SqliteUnitRepository(connection),
            Images: new SqliteImageRepository(connection),
            Pages: new SqlitePageRepository(connection),
            QuickSearch: new SqliteQuickSearch(connection),
            Tags: new SqliteTagRepository(connection),
            Slugs: new SqliteSlugRepository(connection),
            Configuration: new SqliteConfigurationRepository(connection),
            EmailVerifications: new SqliteEmailVerificationTokenStore(connection),
            Translations: translations);
}
