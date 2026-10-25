using Dapper;

namespace FunctionalBlog.Test.Sqlite;

// Verifies migration 0011 normalizes tags into the polymorphic taggables/tags/taggings
// model and that the repositories read/write through it (no free-text recipe_tags table).
public sealed class TagNormalizationTests : IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Drops_recipe_tags_and_creates_the_normalized_tables()
    {
        var tables = _db.Connection
            .Query<string>("SELECT name FROM sqlite_master WHERE type = 'table'")
            .ToHashSet();

        Assert.DoesNotContain("recipe_tags", tables);
        Assert.Contains("taggables", tables);
        Assert.Contains("tags", tables);
        Assert.Contains("taggings", tables);
    }

    [Fact]
    public async Task Saving_two_recipes_with_the_same_tag_stores_the_tag_once()
    {
        var repo = new SqliteRecipeRepository(_db.Connections);
        var first = await repo.NextId();
        var second = await repo.NextId();
        await repo.Save(ARecipe(first, "Suppe", "Vegetarisch"));
        await repo.Save(ARecipe(second, "Eintopf", "Vegetarisch"));

        // The shared tag is stored exactly once (deduped case-insensitively), no matter how many
        // recipes carry it.
        var tagRows = await _db.Connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM tags WHERE name = 'Vegetarisch' COLLATE NOCASE");

        // Both of the two recipes link to that single tag.
        var taggingRows = await _db.Connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM taggings tg
            JOIN tags t ON t.id = tg.tag_id
            JOIN recipes r ON r.taggable_id = tg.taggable_id
            WHERE t.name = 'Vegetarisch' COLLATE NOCASE AND r.id IN (@first, @second)
            """,
            new { first = first.Value, second = second.Value });

        Assert.Equal(1, tagRows);
        Assert.Equal(2, taggingRows);
    }

    [Fact]
    public async Task Every_saved_article_owns_a_taggable()
    {
        var articles = new SqliteArticleRepository(_db.Connections);
        var id = await articles.NextId();
        await articles.Save(AnArticle(id));

        var taggableId = await _db.Connection.ExecuteScalarAsync<long?>(
            "SELECT taggable_id FROM articles WHERE id = @id", new { id = id.Value });

        Assert.NotNull(taggableId);
        var exists = await _db.Connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM taggables WHERE id = @tid", new { tid = taggableId });
        Assert.Equal(1, exists);
    }

    [Fact]
    public async Task Deleting_a_recipe_removes_its_taggable_and_taggings()
    {
        var repo = new SqliteRecipeRepository(_db.Connections);
        var id = await repo.NextId();
        await repo.Save(ARecipe(id, "Wegwerf", "Test"));

        var taggableId = await _db.Connection.ExecuteScalarAsync<long>(
            "SELECT taggable_id FROM recipes WHERE id = @id", new { id = id.Value });

        await repo.Delete(id);

        var taggables = await _db.Connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM taggables WHERE id = @tid", new { tid = taggableId });
        var taggings = await _db.Connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM taggings WHERE taggable_id = @tid", new { tid = taggableId });

        Assert.Equal(0, taggables);
        Assert.Equal(0, taggings);
    }

    [Fact]
    public async Task Suess_tag_has_transliterated_slug_and_tags_the_macaron_article()
    {
        var tags = new SqliteTagRepository(_db.Connections);
        var tag = FunctionalAssert.Some(await tags.FindBySlug("suess"));
        Assert.Equal("süss", tag.Name);

        var articles = new SqliteArticleRepository(_db.Connections);
        var tagged = await articles.FindByTag("suess");
        Assert.Contains(tagged, a => a.Title.Value == "Macarons selbst backen");
    }

    [Fact]
    public void Tags_table_has_no_slug_column_after_folding_into_the_registry()
    {
        var columns = _db.Connection
            .Query<string>("SELECT name FROM pragma_table_info('tags')")
            .ToHashSet();

        Assert.DoesNotContain("slug", columns);
        Assert.Contains("name", columns);
    }

    [Fact]
    public async Task Tag_slugs_live_in_the_central_registry()
    {
        // Migration 0017 copies each tag's slug into the shared `slugs` table under the 'tag' type.
        var entityType = await _db.Connection.ExecuteScalarAsync<string>(
            "SELECT entity_type FROM slugs WHERE slug = 'suess'");

        Assert.Equal("tag", entityType);
    }

    [Fact]
    public async Task FindIdByName_resolves_a_tag_id_case_insensitively()
    {
        var tags = new SqliteTagRepository(_db.Connections);

        // SQLite's NOCASE folds ASCII case, so "Süss" (capital S) resolves to the stored "süss".
        var id = FunctionalAssert.Some(await tags.FindIdByName("Süss"));
        var all = await tags.All();

        Assert.Contains(all, t => t.Id == id && t.Name == "süss");
    }

    private static Recipe ARecipe(RecipeId id, string name, params string[] tags) =>
        Recipe.Create(
            id,
            new RecipeName(name),
            new RecipeDescription("Beschreibung"),
            [],
            new UserId(1),
            Difficulty.Easy,
            tags.Select(t => new RecipeTag(t)).ToList(),
            2,
            [],
            [],
            []);

    private static Article AnArticle(ArticleId id) =>
        Article.Create(
            id,
            new ArticleTitle("Titel"),
            new ArticleTeaser("Ein kurzer Teaser für den Artikel."),
            new ArticleText("Text"),
            new UserId(1),
            new DateTimeOffset(2026, 6, 23, 12, 0, 0, TimeSpan.Zero));
}
