namespace Foodblog.Test.Search;

public sealed class SearchIndexTests : IDisposable
{
    private readonly string _indexPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly LeanCorpusSearchIndex _index;

    public SearchIndexTests()
    {
        _index = new LeanCorpusSearchIndex(_indexPath);
    }

    [Fact]
    public void Search_finds_indexed_article_by_title()
    {
        _index.IndexArticle(AnArticle(id: 1, title: "Macarons backen", body: "Rezept für Macarons"));

        var results = _index.Search("Macarons");

        Assert.Single(results);
        Assert.Equal("article", results[0].Type);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Macarons backen", results[0].Title);
    }

    [Fact]
    public void Search_finds_indexed_recipe_by_body()
    {
        _index.IndexRecipe(ARecipe(id: 2, title: "Pfannkuchen", body: "Mehl Eier Milch verrühren"));

        var results = _index.Search("Eier");

        Assert.Single(results);
        Assert.Equal("recipe", results[0].Type);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Search_finds_indexed_ingredient()
    {
        _index.IndexIngredient(AnIngredient(id: 3, name: "Butter", description: "Süßrahmbutter"));

        var results = _index.Search("Butter");

        Assert.Single(results);
        Assert.Equal("ingredient", results[0].Type);
        Assert.Equal(3, results[0].Id);
    }

    [Fact]
    public void Search_result_snippet_is_not_empty_for_body_match()
    {
        _index.IndexArticle(AnArticle(id: 1, title: "Kuchen", body: "Dieser Kuchen ist sehr lecker und einfach zu backen"));

        var results = _index.Search("lecker");

        Assert.NotEmpty(results[0].Snippet);
    }

    [Fact]
    public void DeleteDocument_removes_article_from_results()
    {
        _index.IndexArticle(AnArticle(id: 1, title: "Zu löschen", body: "Dieser Artikel wird gelöscht"));
        _index.DeleteDocument("article", 1);

        var results = _index.Search("löschen");

        Assert.Empty(results);
    }

    [Fact]
    public void DeleteDocument_does_not_remove_other_types_with_same_id()
    {
        _index.IndexArticle(AnArticle(id: 5, title: "Artikel fünf", body: "Inhalt"));
        _index.IndexRecipe(ARecipe(id: 5, title: "Rezept fünf", body: "Zubereitung"));
        _index.DeleteDocument("article", 5);

        var results = _index.Search("fünf");

        Assert.Single(results);
        Assert.Equal("recipe", results[0].Type);
    }

    [Fact]
    public void Suggestions_returns_close_terms_for_misspelled_query()
    {
        _index.IndexArticle(AnArticle(id: 1, title: "Schokolade", body: "Schokolade Torte Rezept"));

        var suggestions = _index.Suggestions("Shokolade");

        Assert.NotEmpty(suggestions);
    }

    [Fact]
    public async Task RebuildAsync_indexes_all_domain_entities()
    {
        var articles = new InMemoryArticleRepository();
        var recipes = new InMemoryRecipeRepository();
        var ingredients = new InMemoryIngredientRepository();
        var pages = new InMemoryPageRepository();

        var articleId = await articles.NextId();
        await articles.Save(AnArticleEntity(articleId, "Rührkuchen"));

        var recipeId = await recipes.NextId();
        await recipes.Save(ARecipeEntity(recipeId, "Pfannkuchen"));

        var ingredientId = await ingredients.NextId();
        await ingredients.Save(AnIngredientEntity(ingredientId, "Zucker"));

        var pageId = await pages.NextId();
        await pages.Save(APageEntity(pageId, "Impressum"));

        await _index.RebuildAsync(articles, recipes, ingredients, pages);

        Assert.NotEmpty(_index.Search("Rührkuchen"));
        Assert.NotEmpty(_index.Search("Pfannkuchen"));
        Assert.NotEmpty(_index.Search("Zucker"));
        Assert.NotEmpty(_index.Search("Impressum"));
    }

    [Fact]
    public async Task Status_reports_per_type_counts_and_last_rebuilt()
    {
        var articles = new InMemoryArticleRepository();
        var recipes = new InMemoryRecipeRepository();
        var ingredients = new InMemoryIngredientRepository();
        var pages = new InMemoryPageRepository();

        await articles.Save(AnArticleEntity(await articles.NextId(), "Rührkuchen"));
        await recipes.Save(ARecipeEntity(await recipes.NextId(), "Pfannkuchen"));
        await ingredients.Save(AnIngredientEntity(await ingredients.NextId(), "Zucker"));
        await pages.Save(APageEntity(await pages.NextId(), "Impressum"));

        await _index.RebuildAsync(articles, recipes, ingredients, pages);

        var status = _index.Status();

        Assert.Equal((await articles.All()).Count, status.Articles);
        Assert.Equal((await recipes.All()).Count, status.Recipes);
        Assert.Equal((await ingredients.All()).Count, status.Ingredients);
        Assert.Equal((await pages.All()).Count, status.Pages);
        Assert.Equal(status.Articles + status.Recipes + status.Ingredients + status.Pages, status.Total);
        Assert.True(status.Total > 0);
        Assert.True(status.LastRebuilt is [_], "expected LastRebuilt to be set after a rebuild");
    }

    [Fact]
    public async Task IndexPage_makes_a_page_searchable_by_title()
    {
        _index.IndexPage(Page.Create(new PageId(7), new PageTitle("Über uns"), new PageContent("Wir backen seit 1990.")));

        var results = _index.Search("uns");

        Assert.Single(results);
        Assert.Equal("page", results[0].Type);
        Assert.Equal(7, results[0].Id);
        Assert.Equal("Über uns", results[0].Title);
    }

    [Fact]
    public void DeleteDocument_removes_a_page_from_results()
    {
        _index.IndexPage(Page.Create(new PageId(3), new PageTitle("Zu löschen"), new PageContent("Diese Seite wird gelöscht.")));
        _index.DeleteDocument("page", 3);

        Assert.Empty(_index.Search("löschen"));
    }

    [Fact]
    public async Task Restarting_over_same_directory_does_not_accumulate_orphaned_segments()
    {
        var indexPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var articles = new InMemoryArticleRepository();
        var recipes = new InMemoryRecipeRepository();
        var ingredients = new InMemoryIngredientRepository();
        var pages = new InMemoryPageRepository();

        var articleId = await articles.NextId();
        await articles.Save(AnArticleEntity(articleId, "Rührkuchen"));

        try
        {
            // Simulate many application restarts over the same persisted index directory.
            for (var restart = 0; restart < 25; restart++)
            {
                using var index = new LeanCorpusSearchIndex(indexPath);
                await index.RebuildAsync(articles, recipes, ingredients, pages);
                Assert.NotEmpty(index.Search("Rührkuchen"));
            }

            // A from-scratch rebuild must not leave the directory growing without bound:
            // orphaned segment files from prior runs are what climbed the production index
            // to seg_186 and caused the file-lock crash on startup.
            var segmentCount = Directory.GetFiles(indexPath, "seg_*.pos").Length;
            Assert.True(segmentCount <= 5, $"Expected a bounded segment count after restarts, but found {segmentCount}.");
        }
        finally
        {
            if (Directory.Exists(indexPath))
            {
                Directory.Delete(indexPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Rebuild_makes_the_index_match_the_repositories()
    {
        // A doc that exists only in the live index (e.g. left over after a failed commit / drift).
        _index.IndexArticle(AnArticle(99, "Geisterartikel", "verwaister Eintrag"));

        var articles = new InMemoryArticleRepository();
        var id = await articles.NextId();
        await articles.Save(AnArticleEntity(id, "Bratapfel"));

        await _index.RebuildAsync(
            articles,
            new InMemoryRecipeRepository(),
            new InMemoryIngredientRepository(),
            new InMemoryPageRepository());

        Assert.NotEmpty(_index.Search("Bratapfel"));      // repository content is present
        Assert.Empty(_index.Search("Geisterartikel"));    // drift is healed away
    }

    [Fact]
    public async Task Concurrent_search_index_and_rebuild_are_serialised_safely()
    {
        var articles = new InMemoryArticleRepository();
        for (var i = 1; i <= 5; i++)
        {
            var id = await articles.NextId();
            await articles.Save(AnArticleEntity(id, $"Stollen {i}"));
        }

        // Exercises the gate: concurrent writes/searches/rebuild must not corrupt or throw. Kept
        // to a handful of commits so the run stays below the library's merge threshold — this is a
        // thread-safety test, not a stress test of LeanCorpus's segment merging under churn.
        var tasks = new List<Task>();
        for (var i = 0; i < 6; i++)
        {
            var n = i;
            tasks.Add(Task.Run(() => _index.IndexArticle(AnArticle(1000 + n, $"Parallel {n}", "Inhalt"))));
            tasks.Add(Task.Run(() => _index.Search("Parallel")));
        }

        tasks.Add(Task.Run(async () => await _index.RebuildAsync(
            articles,
            new InMemoryRecipeRepository(),
            new InMemoryIngredientRepository(),
            new InMemoryPageRepository())));

        await Task.WhenAll(tasks);

        Assert.NotEmpty(_index.Search("Stollen"));
    }

    public void Dispose()
    {
        _index.Dispose();
        if (Directory.Exists(_indexPath))
        {
            Directory.Delete(_indexPath, recursive: true);
        }
    }

    private static Article AnArticle(int id, string title, string body) =>
        Article.Create(
            new ArticleId(id),
            new ArticleTitle(title),
            new ArticleTeaser(body),
            new ArticleText(body),
            new UserId(1),
            DateTimeOffset.UtcNow);

    private static Recipe ARecipe(int id, string title, string body) =>
        Recipe.Create(
            new RecipeId(id),
            new RecipeName(title),
            new RecipeDescription(body),
            [],
            new UserId(1),
            Difficulty.Easy,
            [],
            2,
            [],
            [],
            []);

    private static Ingredient AnIngredient(int id, string name, string description) =>
        Ingredient.Create(
            new IngredientId(id),
            new IngredientName(name),
            string.Empty,
            description,
            density: 1m,
            pieceCount: 0m,
            calorificValue: 0m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m);

    private static Article AnArticleEntity(ArticleId id, string title) =>
        AnArticle(id.Value, title, $"Beschreibung von {title}");

    private static Recipe ARecipeEntity(RecipeId id, string name) =>
        ARecipe(id.Value, name, $"Zubereitung von {name}");

    private static Ingredient AnIngredientEntity(IngredientId id, string name) =>
        AnIngredient(id.Value, name, $"Beschreibung von {name}");

    private static Page APageEntity(PageId id, string title) =>
        Page.Create(id, new PageTitle(title), new PageContent($"Inhalt von {title}"));
}
