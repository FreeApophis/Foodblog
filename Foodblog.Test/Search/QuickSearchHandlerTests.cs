namespace Foodblog.Test.Search;

public sealed class QuickSearchHandlerTests
{
    [Fact]
    public async Task Quick_returns_empty_body_for_a_blank_query()
    {
        var env = await BuildEnv();

        var response = await SearchHandlers.Quick(ARequest(string.Empty))(env);

        Assert.Equal(string.Empty, response.Body);
    }

    [Fact]
    public async Task Quick_groups_matches_under_translated_category_headings()
    {
        var env = await BuildEnv();

        var response = await SearchHandlers.Quick(ARequest("beere"))(env);

        Assert.Contains("search.type.tag", response.Body);
        Assert.Contains("search.type.article", response.Body);
        Assert.Contains("search.type.recipe", response.Body);
        Assert.Contains("search.type.ingredient", response.Body);
    }

    [Fact]
    public async Task Quick_links_tags_articles_and_recipes_to_their_pages()
    {
        var env = await BuildEnv();

        var response = await SearchHandlers.Quick(ARequest("beere"))(env);

        Assert.Contains("href=\"/tag/beere\"", response.Body);
        Assert.Contains("href=\"/articles/1\"", response.Body);
        Assert.Contains("href=\"/recipes/1\"", response.Body);
    }

    [Fact]
    public async Task Quick_links_ingredients_to_their_detail_page()
    {
        var env = await BuildEnv();

        var response = await SearchHandlers.Quick(ARequest("beerenmix"))(env);

        Assert.Contains("Beerenmix", response.Body);
        Assert.Contains("href=\"/ingredients/1\"", response.Body);
    }

    [Fact]
    public async Task Quick_shows_a_no_results_message_when_nothing_matches()
    {
        var env = await BuildEnv();

        var response = await SearchHandlers.Quick(ARequest("garnichts"))(env);

        Assert.Contains("search.no_results", response.Body);
    }

    private static async Task<Env> BuildEnv()
    {
        var articles = new InMemoryArticleRepository();
        var recipes = new InMemoryRecipeRepository();
        var ingredients = new InMemoryIngredientRepository();

        await articles.Save(Article.Create(
            new ArticleId(1),
            new ArticleTitle("Beeren pflücken"),
            new ArticleTeaser("Teaser."),
            new ArticleText("Sommer."),
            new UserId(1),
            DateTimeOffset.UtcNow));

        await recipes.Save(Recipe.Create(
            new RecipeId(1),
            new RecipeName("Beerenkuchen"),
            new RecipeDescription("lecker"),
            [new PreparationStep(1, "Schritt.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            4,
            [],
            [],
            []));

        await ingredients.Save(Ingredient.Create(
            new IngredientId(1),
            new IngredientName("Beerenmix"),
            image: string.Empty,
            description: string.Empty,
            density: 1m,
            pieceCount: 0m,
            calorificValue: 0m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m));

        var quickSearch = new InMemoryQuickSearch(articles, recipes, ingredients, new Tag("beere", "Beere"));

        return new Env(
            Articles: articles,
            Users: new InMemoryUserRepository(),
            Roles: new InMemoryRoleRepository(),
            Sessions: new InMemorySessionStore(),
            PasswordResets: new InMemoryPasswordResetTokenStore(),
            PasswordHasher: new Pbkdf2PasswordHasher(),
            Clock: new SystemClock(),
            Log: new ConsoleLog(),
            CurrentUser: Guest.Instance,
            Recipes: recipes,
            Ingredients: ingredients,
            Units: new InMemoryUnitRepository(),
            Images: new InMemoryImageRepository(),
            Pages: new InMemoryPageRepository(),
            QuickSearch: quickSearch);
    }

    private static Request ARequest(string q) =>
        new(HttpMethod.Get, "/search/quick", Empty, new Dictionary<string, string> { ["q"] = q }, Empty, Empty);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
