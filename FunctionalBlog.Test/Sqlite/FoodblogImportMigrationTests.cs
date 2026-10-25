namespace FunctionalBlog.Test.Sqlite;

// Verifies the 0002_seed migration loads the curated foodblog content into a
// freshly-migrated database.
public sealed class FoodblogImportMigrationTests : IDisposable
{
    private readonly SqliteTestBase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Imports_the_four_blog_articles()
    {
        var articles = new SqliteArticleRepository(_db.Connections);

        var all = await articles.All();

        Assert.Equal(4, all.Count);
        Assert.Contains(all, a => a.Title.Value == "Macarons selbst backen");
        Assert.Contains(all, a => a.Title.Value == "Willkommen auf Foodblog.ch");
    }

    [Fact]
    public async Task Imports_all_recipes_with_parseable_units()
    {
        var recipes = new SqliteRecipeRepository(_db.Connections);

        // All() joins each recipe_ingredient to units; every unit_id must resolve to a seeded unit.
        var all = await recipes.All();
        var unitIds = (await new SqliteUnitRepository(_db.Connections).All()).Select(u => u.Id.Value).ToHashSet();

        Assert.Equal(36, all.Count);
        Assert.Contains(all, r => r.Name.Value == "Älpler One-Pot");
        Assert.Contains(all, r => r.Name.Value == "The Hamburger");
        Assert.All(all, r => Assert.All(r.Ingredients, i => Assert.Contains(i.Unit.Id.Value, unitIds)));
    }

    [Fact]
    public async Task Imports_all_ingredients()
    {
        var ingredients = new SqliteIngredientRepository(_db.Connections);

        var all = await ingredients.All();

        // The duplicate placeholder "Apfel" (id 141) was dropped before the seed was snapshotted.
        Assert.Equal(153, all.Count);
    }

    [Fact]
    public async Task Imports_the_two_authors_with_admin_role()
    {
        var users = new SqliteUserRepository(_db.Connections);

        // The two content authors are seeded first (Thomas id 1, Sabrina id 2); the dedicated
        // admin@blog.de account follows at id 3.
        var thomas = FunctionalAssert.Some(await users.FindById(new UserId(1)));
        var sabrina = FunctionalAssert.Some(await users.FindById(new UserId(2)));

        Assert.Equal("Thomas", thomas!.DisplayName.Value);
        Assert.Equal("Sabrina", sabrina!.DisplayName.Value);
        Assert.Contains("Admin", thomas.RoleNames);
        Assert.Contains("Admin", sabrina.RoleNames);
    }

    [Fact]
    public async Task Imports_recipe_steps_tags_and_hints()
    {
        var recipes = new SqliteRecipeRepository(_db.Connections);

        var rhabarber = FunctionalAssert.Some(await recipes.Find(new RecipeId(9)));

        Assert.NotEmpty(rhabarber!.PreparationSteps);
        Assert.Contains(rhabarber.Tags, t => t.Value == "rhabarber");
    }

    [Fact]
    public async Task Decodes_html_entities_in_text()
    {
        var recipes = new SqliteRecipeRepository(_db.Connections);

        var all = await recipes.All();

        // Source text is full of entities like &uuml; / &amp; — none should survive decoding.
        foreach (var entity in new[] { "&uuml;", "&ouml;", "&auml;", "&amp;", "&nbsp;" })
        {
            Assert.All(all, r => Assert.DoesNotContain(entity, r.Description.Value));
            Assert.All(all, r => Assert.All(r.PreparationSteps, s => Assert.DoesNotContain(entity, s.Text)));
        }
    }
}
