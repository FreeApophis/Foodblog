using System.Data;
using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteRecipeRepository : IRecipeRepository
{
    // recipe_ingredients joined to units so each ingredient line hydrates a full Unit.
    private const string IngredientSelect =
        "SELECT ri.recipe_id AS RecipeId, ri.sort_order AS SortOrder, ri.ingredient_id AS IngredientId, ri.amount AS Amount, " +
        "ri.unit_id AS UnitId, u.category AS Category, u.unit_factor AS Factor, u.name_key AS NameKey, u.abbreviation_key AS AbbreviationKey " +
        "FROM recipe_ingredients ri JOIN units u ON u.id = ri.unit_id";

    // Tags reached through the recipe's 1:1 taggable — every join is on an integer key.
    private const string TagSelect =
        "SELECT r.id AS RecipeId, t.name AS Tag " +
        "FROM recipes r JOIN taggings tg ON tg.taggable_id = r.taggable_id JOIN tags t ON t.id = tg.tag_id";

    private const string RecipeSelect =
        "SELECT id AS Id, name AS Name, description AS Description, author_id AS AuthorId, difficulty AS Difficulty, portions AS Portions, preparation_time AS PreparationTime, cooking_time AS CookingTime, calorific_value AS CalorificValue FROM recipes";

    private readonly SqliteConnectionFactory _connections;
    private int _nextId = -1;

    public SqliteRecipeRepository(SqliteConnectionFactory connections) => _connections = connections;

    public async ValueTask<IReadOnlyList<Recipe>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = (await connection.QueryAsync<RecipeRow>(RecipeSelect)).ToList();
        return await Hydrate(connection, rows);
    }

    public async ValueTask<IReadOnlyList<Recipe>> FindByTag(string slug)
    {
        await using var connection = await _connections.OpenAsync();
        var rows = (await connection.QueryAsync<RecipeRow>(
            $"{RecipeSelect} WHERE taggable_id IN " +
            "(SELECT tg.taggable_id FROM taggings tg " +
            "JOIN slugs s ON s.entity_type = 'tag' AND s.entity_id = tg.tag_id WHERE s.slug = @slug) " +
            "ORDER BY name",
            new { slug })).ToList();
        return await Hydrate(connection, rows);
    }

    public async ValueTask<Option<Recipe>> Find(RecipeId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<RecipeRow>(
            "SELECT id AS Id, name AS Name, description AS Description, author_id AS AuthorId, difficulty AS Difficulty, portions AS Portions, preparation_time AS PreparationTime, cooking_time AS CookingTime, calorific_value AS CalorificValue FROM recipes WHERE id = @id",
            new { id = id.Value });

        if (row is null)
        {
            return Option<Recipe>.None;
        }

        var steps = (await connection.QueryAsync<StepRow>(
            "SELECT recipe_id AS RecipeId, sort_order AS SortOrder, text AS Text FROM recipe_steps WHERE recipe_id = @id ORDER BY sort_order",
            new { id = id.Value })).ToList();

        var tags = (await connection.QueryAsync<TagRow>(
            TagSelect + " WHERE r.id = @id ORDER BY t.name",
            new { id = id.Value })).ToList();

        var ingredients = (await connection.QueryAsync<IngredientRow>(
            $"{IngredientSelect} WHERE ri.recipe_id = @id ORDER BY ri.sort_order",
            new { id = id.Value })).ToList();

        var images = (await connection.QueryAsync<ImageRow>(
            "SELECT recipe_id AS RecipeId, url AS Url FROM recipe_images WHERE recipe_id = @id ORDER BY sort_order",
            new { id = id.Value })).ToList();

        var hints = (await connection.QueryAsync<HintRow>(
            "SELECT recipe_id AS RecipeId, text AS Text FROM recipe_hints WHERE recipe_id = @id ORDER BY sort_order",
            new { id = id.Value })).ToList();

        return Option.Some(BuildRecipe(row, steps, tags, ingredients, images, hints));
    }

    public async ValueTask<RecipeId> NextId()
    {
        await using var connection = await _connections.OpenAsync();
        if (_nextId < 0)
        {
            _nextId = await connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(id), 0) FROM recipes");
        }

        return new RecipeId(++_nextId);
    }

    public async ValueTask Save(Recipe recipe)
    {
        await using var connection = await _connections.OpenAsync();
        using var transaction = connection.BeginTransaction();

        // Reuse the recipe's existing taggable on update, mint a fresh one on insert.
        // Read it before the INSERT OR REPLACE below deletes the old row.
        var taggableId = await EnsureTaggable(connection, recipe.Id.Value, transaction);

        await connection.ExecuteAsync(
            """
            INSERT OR REPLACE INTO recipes (id, name, description, author_id, difficulty, portions, preparation_time, cooking_time, calorific_value, taggable_id)
            VALUES (@Id, @Name, @Description, @AuthorId, @Difficulty, @Portions, @PreparationTime, @CookingTime, @CalorificValue, @TaggableId)
            """,
            new
            {
                Id = recipe.Id.Value,
                Name = recipe.Name.Value,
                Description = recipe.Description.Value,
                AuthorId = recipe.AuthorId.Value,
                Difficulty = (int)recipe.Difficulty,
                recipe.Portions,
                recipe.PreparationTime,
                recipe.CookingTime,
                recipe.CalorificValue,
                TaggableId = taggableId,
            },
            transaction);

        if (recipe.PreparationSteps.Count > 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO recipe_steps (recipe_id, sort_order, text) VALUES (@RecipeId, @SortOrder, @Text)",
                recipe.PreparationSteps.Select(s => new { RecipeId = recipe.Id.Value, s.SortOrder, s.Text }),
                transaction);
        }

        // Re-sync the taggable's tags: clear, then upsert each tag and link it.
        await connection.ExecuteAsync(
            "DELETE FROM taggings WHERE taggable_id = @TaggableId",
            new { TaggableId = taggableId },
            transaction);

        if (recipe.Tags.Count > 0)
        {
            // Tags now dedup on their display name (the URL slug lives in the central `slugs`
            // registry, assigned by SlugService after the save). Insert each unseen name, then link.
            var tagParams = recipe.Tags
                .Select(t => t.Value.Trim())
                .Where(name => name.Length > 0)
                .Select(name => new { TaggableId = taggableId, Name = name })
                .ToList();

            await connection.ExecuteAsync(
                "INSERT INTO tags (name) SELECT @Name " +
                "WHERE NOT EXISTS (SELECT 1 FROM tags WHERE name = @Name COLLATE NOCASE)",
                tagParams,
                transaction);

            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO taggings (taggable_id, tag_id) " +
                "SELECT @TaggableId, id FROM tags WHERE name = @Name COLLATE NOCASE",
                tagParams,
                transaction);
        }

        if (recipe.Ingredients.Count > 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO recipe_ingredients (recipe_id, sort_order, ingredient_id, amount, unit_id) VALUES (@RecipeId, @SortOrder, @IngredientId, @Amount, @UnitId)",
                recipe.Ingredients.Select((ri, i) => new
                {
                    RecipeId = recipe.Id.Value,
                    SortOrder = i + 1,
                    IngredientId = ri.IngredientId.Value,
                    ri.Amount,
                    UnitId = ri.Unit.Id.Value,
                }),
                transaction);
        }

        if (recipe.Images.Count > 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO recipe_images (recipe_id, sort_order, url) VALUES (@RecipeId, @SortOrder, @Url)",
                recipe.Images.Select((url, i) => new { RecipeId = recipe.Id.Value, SortOrder = i + 1, Url = url }),
                transaction);
        }

        if (recipe.Hints.Count > 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO recipe_hints (recipe_id, sort_order, text) VALUES (@RecipeId, @SortOrder, @Text)",
                recipe.Hints.Select((h, i) => new { RecipeId = recipe.Id.Value, SortOrder = i + 1, h.Text }),
                transaction);
        }

        transaction.Commit();
    }

    public async ValueTask UpdateCalorificValue(RecipeId id, int value)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            "UPDATE recipes SET calorific_value = @value WHERE id = @id",
            new { id = id.Value, value });
    }

    public async ValueTask Delete(RecipeId id)
    {
        await using var connection = await _connections.OpenAsync();
        using var transaction = connection.BeginTransaction();

        var taggableId = await connection.ExecuteScalarAsync<long?>(
            "SELECT taggable_id FROM recipes WHERE id = @id", new { id = id.Value }, transaction);

        await connection.ExecuteAsync("DELETE FROM recipes WHERE id = @id", new { id = id.Value }, transaction);

        // Removing the owned taggable cascades to its taggings.
        if (taggableId is { } owned)
        {
            await connection.ExecuteAsync(
                "DELETE FROM taggables WHERE id = @owned", new { owned }, transaction);
        }

        transaction.Commit();
    }

    // Returns the recipe's taggable id, minting a new taggable when the recipe is new.
    private async Task<long> EnsureTaggable(IDbConnection connection, int recipeId, IDbTransaction transaction)
    {
        var existing = await connection.ExecuteScalarAsync<long?>(
            "SELECT taggable_id FROM recipes WHERE id = @id", new { id = recipeId }, transaction);

        if (existing is { } taggableId)
        {
            return taggableId;
        }

        await connection.ExecuteAsync("INSERT INTO taggables DEFAULT VALUES", transaction: transaction);
        return await connection.ExecuteScalarAsync<long>("SELECT last_insert_rowid()", transaction: transaction);
    }

    // Loads the child rows (steps, tags, ingredients, images, hints) for a set of recipe rows
    // and assembles full Recipe aggregates.
    private async Task<IReadOnlyList<Recipe>> Hydrate(IDbConnection connection, IReadOnlyList<RecipeRow> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(r => r.Id).ToList();

        var steps = (await connection.QueryAsync<StepRow>(
            "SELECT recipe_id AS RecipeId, sort_order AS SortOrder, text AS Text FROM recipe_steps WHERE recipe_id IN @ids ORDER BY sort_order",
            new { ids })).ToLookup(s => s.RecipeId);

        var tags = (await connection.QueryAsync<TagRow>(
            TagSelect + " WHERE r.id IN @ids ORDER BY t.name",
            new { ids })).ToLookup(t => t.RecipeId);

        var ingredients = (await connection.QueryAsync<IngredientRow>(
            $"{IngredientSelect} WHERE ri.recipe_id IN @ids ORDER BY ri.sort_order",
            new { ids })).ToLookup(i => i.RecipeId);

        var images = (await connection.QueryAsync<ImageRow>(
            "SELECT recipe_id AS RecipeId, url AS Url FROM recipe_images WHERE recipe_id IN @ids ORDER BY sort_order",
            new { ids })).ToLookup(i => i.RecipeId);

        var hints = (await connection.QueryAsync<HintRow>(
            "SELECT recipe_id AS RecipeId, text AS Text FROM recipe_hints WHERE recipe_id IN @ids ORDER BY sort_order",
            new { ids })).ToLookup(h => h.RecipeId);

        return rows.Select(r => BuildRecipe(
            r,
            steps[r.Id].ToList(),
            tags[r.Id].ToList(),
            ingredients[r.Id].ToList(),
            images[r.Id].ToList(),
            hints[r.Id].ToList())).ToList();
    }

    private static Recipe BuildRecipe(
        RecipeRow row,
        IReadOnlyList<StepRow> steps,
        IReadOnlyList<TagRow> tags,
        IReadOnlyList<IngredientRow> ingredients,
        IReadOnlyList<ImageRow> images,
        IReadOnlyList<HintRow> hints) =>
        Recipe.Create(
            new RecipeId((int)row.Id),
            new RecipeName(row.Name),
            new RecipeDescription(row.Description),
            steps.Select(s => new PreparationStep((int)s.SortOrder, s.Text)).ToList(),
            new UserId((int)row.AuthorId),
            (Difficulty)row.Difficulty,
            tags.Select(t => new RecipeTag(t.Tag)).ToList(),
            (int)row.Portions,
            ingredients.Select(i => new RecipeIngredient(
                new IngredientId((int)i.IngredientId),
                i.Amount,
                new FunctionalBlog.Domain.Recipes.Unit(
                    new UnitId((int)i.UnitId),
                    i.NameKey,
                    i.AbbreviationKey,
                    (UnitCategory)i.Category,
                    i.Factor))).ToList(),
            images.Select(i => i.Url).ToList(),
            hints.Select(h => new RecipeHint(h.Text)).ToList(),
            (int)row.PreparationTime,
            (int)row.CookingTime,
            (int)row.CalorificValue);

    private sealed record RecipeRow(
        long Id,
        string Name,
        string Description,
        long AuthorId,
        long Difficulty,
        long Portions,
        long PreparationTime,
        long CookingTime,
        long CalorificValue);

    private sealed record StepRow(long RecipeId, long SortOrder, string Text);

    private sealed record TagRow(long RecipeId, string Tag);

    private sealed record IngredientRow(
        long RecipeId,
        long SortOrder,
        long IngredientId,
        decimal Amount,
        long UnitId,
        long Category,
        decimal Factor,
        string NameKey,
        string AbbreviationKey);

    private sealed record ImageRow(long RecipeId, string Url);

    private sealed record HintRow(long RecipeId, string Text);
}
