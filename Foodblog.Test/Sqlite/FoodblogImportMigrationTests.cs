namespace Foodblog.Test.Sqlite;

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
    public async Task Imports_only_the_initial_admin_without_a_password()
    {
        var users = new SqliteUserRepository(_db.Connections);

        // No real accounts ship with the seed: a single placeholder admin owns all content and gets
        // its (one-time, logged) password from the startup seeder.
        var admin = Assert.Single(await users.All());

        Assert.Equal(new UserId(1), admin.Id);
        Assert.Equal("admin@blog.de", admin.Email.Value);
        Assert.Contains("Admin", admin.RoleNames);
        Assert.Equal(string.Empty, admin.PasswordHash);
    }

    [Fact]
    public async Task All_articles_and_recipes_belong_to_the_initial_admin()
    {
        var articles = await new SqliteArticleRepository(_db.Connections).All();
        var recipes = await new SqliteRecipeRepository(_db.Connections).All();

        Assert.All(articles, a => Assert.Equal(new UserId(1), a.AuthorId));
        Assert.All(recipes, r => Assert.Equal(new UserId(1), r.AuthorId));
    }

    [Fact]
    public async Task Imports_no_pages()
    {
        var pages = new SqlitePageRepository(_db.Connections);

        Assert.Empty(await pages.All());
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
