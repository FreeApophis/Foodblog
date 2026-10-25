using Dapper;

namespace FunctionalBlog.DataAccess.Sqlite;

public sealed class SqliteIngredientRepository : IIngredientRepository
{
    private readonly SqliteConnectionFactory _connections;
    private readonly IdSequence _ids;

    public SqliteIngredientRepository(SqliteConnectionFactory connections)
    {
        _connections = connections;
        _ids = new IdSequence(connections, "ingredients");
    }

    public async ValueTask<IReadOnlyList<Ingredient>> All()
    {
        await using var connection = await _connections.OpenAsync();
        var rows = await connection.QueryAsync<IngredientRow>(
            "SELECT id AS Id, name AS Name, image AS Image, description AS Description, density AS Density, piece_count AS PieceCount, calorific_value AS CalorificValue, protein AS Protein, fat AS Fat, carbohydrates AS Carbohydrates, sugar AS Sugar, fiber AS Fiber FROM ingredients ORDER BY name COLLATE NOCASE");
        return rows.Select(ToIngredient).ToList();
    }

    public async ValueTask<Option<Ingredient>> Find(IngredientId id)
    {
        await using var connection = await _connections.OpenAsync();
        var row = await connection.QuerySingleOrDefaultAsync<IngredientRow>(
            "SELECT id AS Id, name AS Name, image AS Image, description AS Description, density AS Density, piece_count AS PieceCount, calorific_value AS CalorificValue, protein AS Protein, fat AS Fat, carbohydrates AS Carbohydrates, sugar AS Sugar, fiber AS Fiber FROM ingredients WHERE id = @id",
            new { id = id.Value });

        return Option.FromNullable(row).Select(ToIngredient);
    }

    public async ValueTask<IngredientId> NextId() =>
        new(await _ids.Next());

    public async ValueTask Save(Ingredient ingredient)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT OR REPLACE INTO ingredients (id, name, image, description, density, piece_count, calorific_value, protein, fat, carbohydrates, sugar, fiber)
            VALUES (@Id, @Name, @Image, @Description, @Density, @PieceCount, @CalorificValue, @Protein, @Fat, @Carbohydrates, @Sugar, @Fiber)
            """,
            new
            {
                Id = ingredient.Id.Value,
                Name = ingredient.Name.Value,
                ingredient.Image,
                ingredient.Description,
                ingredient.Density,
                ingredient.PieceCount,
                ingredient.CalorificValue,
                ingredient.Protein,
                ingredient.Fat,
                ingredient.Carbohydrates,
                ingredient.Sugar,
                ingredient.Fiber,
            });
    }

    public async ValueTask Delete(IngredientId id)
    {
        await using var connection = await _connections.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM ingredients WHERE id = @id", new { id = id.Value });
    }

    private static Ingredient ToIngredient(IngredientRow row) =>
        Ingredient.Create(
            new IngredientId((int)row.Id),
            new IngredientName(row.Name),
            row.Image,
            row.Description,
            row.Density,
            row.PieceCount,
            row.CalorificValue,
            row.Protein,
            row.Fat,
            row.Carbohydrates,
            row.Sugar,
            row.Fiber);

    private sealed record IngredientRow(
        long Id,
        string Name,
        string Image,
        string Description,
        decimal Density,
        decimal PieceCount,
        decimal CalorificValue,
        decimal Protein,
        decimal Fat,
        decimal Carbohydrates,
        decimal Sugar,
        decimal Fiber);
}
