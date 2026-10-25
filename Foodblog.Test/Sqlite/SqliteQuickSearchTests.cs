using Dapper;

namespace Foodblog.Test.Sqlite;

// The shared test database is migrated with the imported foodblog dataset, so every fixture uses a
// distinctive "Qx" token and high ids to stay isolated from that pre-seeded content.
public sealed class SqliteQuickSearchTests : IDisposable
{
    private readonly SqliteTestBase _db = new();

    [Fact]
    public async Task Search_returns_nothing_for_a_blank_term()
    {
        Assert.Empty(await Subject().Search("   "));
    }

    [Fact]
    public async Task Search_matches_tags_by_name_substring()
    {
        await SeedTag("QxSchokolade", "qx-schokolade");
        await SeedTag("QxVegan", "qx-vegan");

        var hits = await Subject().Search("qxschoko");

        var hit = Assert.Single(hits);
        Assert.Equal("tag", hit.Category);
        Assert.Equal("QxSchokolade", hit.Label);
        Assert.Equal("qx-schokolade", hit.Slug);
    }

    [Fact]
    public async Task Search_returns_at_most_two_tags_ordered_by_name()
    {
        await SeedTag("QxApfel", "qx-apfel");
        await SeedTag("QxAprikose", "qx-aprikose");
        await SeedTag("QxAnanas", "qx-ananas");

        var tags = (await Subject().Search("qx")).Where(h => h.Category == "tag").ToList();

        Assert.Equal(2, tags.Count);
        Assert.Equal(["QxAnanas", "QxApfel"], tags.Select(h => h.Label));
    }

    [Fact]
    public async Task Search_matches_articles_by_title_or_text_and_links_by_id()
    {
        await SeedArticle(9001, "QxBirne Kuchen", "saftig");
        await SeedArticle(9002, "QxWochenende", "wir backen QxBirne");
        await SeedArticle(9003, "QxPasta", "Tomaten");

        var articles = (await Subject().Search("qxbirne")).Where(h => h.Category == "article").ToList();

        Assert.Equal(["QxBirne Kuchen", "QxWochenende"], articles.Select(h => h.Label));
        Assert.Equal(9001, articles[0].Id);
    }

    [Fact]
    public async Task Search_returns_at_most_three_articles_ordered_by_title()
    {
        await SeedArticle(9010, "QxKuchen D", "Lecker.");
        await SeedArticle(9011, "QxKuchen C", "Lecker.");
        await SeedArticle(9012, "QxKuchen B", "Lecker.");
        await SeedArticle(9013, "QxKuchen A", "Lecker.");

        var articles = (await Subject().Search("qxkuchen")).Where(h => h.Category == "article").ToList();

        Assert.Equal(3, articles.Count);
        Assert.Equal(["QxKuchen A", "QxKuchen B", "QxKuchen C"], articles.Select(h => h.Label));
    }

    [Fact]
    public async Task Search_matches_recipes_by_name_only_and_links_by_id()
    {
        await SeedRecipe(9001, "QxApfelkuchen", "mit QxBirne im Text");
        await SeedRecipe(9002, "QxBirnentarte", "schlicht");

        var recipes = (await Subject().Search("qxbirne")).Where(h => h.Category == "recipe").ToList();

        var hit = Assert.Single(recipes);
        Assert.Equal("QxBirnentarte", hit.Label);
        Assert.Equal(9002, hit.Id);
    }

    [Fact]
    public async Task Search_matches_ingredients_by_name_and_links_by_id()
    {
        await SeedIngredient(9001, "QxMehl");
        await SeedIngredient(9002, "QxZucker");

        var ingredients = (await Subject().Search("qxmehl")).Where(h => h.Category == "ingredient").ToList();

        var hit = Assert.Single(ingredients);
        Assert.Equal("QxMehl", hit.Label);
        Assert.Equal(9001, hit.Id);
        Assert.Null(hit.Slug);
    }

    [Fact]
    public async Task Search_orders_categories_tags_then_articles_then_recipes_then_ingredients()
    {
        await SeedTag("QxBeere", "qx-beere");
        await SeedArticle(9001, "QxBeere Artikel", "Sommer.");
        await SeedRecipe(9001, "QxBeere Rezept", "lecker");
        await SeedIngredient(9001, "QxBeere");

        var categories = (await Subject().Search("qxbeere")).Select(h => h.Category).ToList();

        Assert.Equal(["tag", "article", "recipe", "ingredient"], categories);
    }

    [Fact]
    public async Task Search_treats_wildcard_characters_literally()
    {
        await SeedIngredient(9001, "QxMehl");

        // The % is escaped, so it must match a literal percent sign — not act as a wildcard that
        // would otherwise let "qx%hl" match "QxMehl".
        Assert.DoesNotContain(await Subject().Search("qx%hl"), h => h.Label == "QxMehl");
    }

    public void Dispose() => _db.Dispose();

    private SqliteQuickSearch Subject() => new(_db.Connections);

    private async Task SeedTag(string name, string slug)
    {
        await _db.Connection.ExecuteAsync("INSERT INTO tags (name) VALUES (@name)", new { name });
        var id = await _db.Connection.ExecuteScalarAsync<long>("SELECT last_insert_rowid()");
        await _db.Connection.ExecuteAsync(
            "INSERT INTO slugs (slug, entity_type, entity_id) VALUES (@slug, 'tag', @id)", new { slug, id });
    }

    private async Task SeedArticle(int id, string title, string text)
    {
        var repo = new SqliteArticleRepository(_db.Connections);
        await repo.Save(Article.Create(
            new ArticleId(id),
            new ArticleTitle(title),
            new ArticleTeaser("Teaser."),
            new ArticleText(text),
            new UserId(1),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    private async Task SeedRecipe(int id, string name, string description)
    {
        var repo = new SqliteRecipeRepository(_db.Connections);
        await repo.Save(Recipe.Create(
            new RecipeId(id),
            new RecipeName(name),
            new RecipeDescription(description),
            [new PreparationStep(1, "Schritt.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            4,
            [],
            [],
            []));
    }

    private async Task SeedIngredient(int id, string name)
    {
        var repo = new SqliteIngredientRepository(_db.Connections);
        await repo.Save(Ingredient.Create(
            new IngredientId(id),
            new IngredientName(name),
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
    }
}
