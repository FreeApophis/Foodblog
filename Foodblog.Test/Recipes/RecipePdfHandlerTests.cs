using System.Text;

namespace Foodblog.Test.Recipes;

public sealed class RecipePdfHandlerTests
{
    [Fact]
    public async Task Download_returns_a_pdf_attachment()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env);

        var response = await RecipePdfHandlers.Download(id)(ARequest())(env);

        Assert.Equal(200, response.Status);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.Contains("attachment", response.Headers["Content-Disposition"]);
        Assert.Contains(".pdf", response.Headers["Content-Disposition"]);
        Assert.NotNull(response.Binary);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(response.Binary!, 0, 4));
    }

    [Fact]
    public async Task Download_renders_the_alternative_design_when_requested()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env);

        var response = await RecipePdfHandlers.Download(id)(ARequest(("design", "alternative")))(env);

        Assert.Equal(200, response.Status);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(response.Binary!, 0, 4));
    }

    [Fact]
    public async Task Download_returns_404_for_an_unknown_recipe()
    {
        var env = BuildEnv();

        var response = await RecipePdfHandlers.Download(new RecipeId(9999))(ARequest())(env);

        Assert.Equal(404, response.Status);
    }

    private static async Task<RecipeId> SeedRecipe(Env env)
    {
        await env.Users.Save(User.Create(
            new UserId(1),
            new Email("sabrina@blog.de"),
            new DisplayName("Sabrina"),
            "hash",
            [],
            DateTimeOffset.UtcNow));

        var mehl = Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Mehl"),
            image: string.Empty,
            description: string.Empty,
            density: 1m,
            pieceCount: 0m,
            calorificValue: 0m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m);
        await env.Ingredients.Save(mehl);

        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Crispy Chicken"),
            new RecipeDescription("Knusprig und fruchtig."),
            [new PreparationStep(1, "Reis kochen."), new PreparationStep(2, "Panieren und braten.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            2,
            [new RecipeIngredient(mehl.Id, 200m, Gram)],
            [],
            []));
        return id;
    }

    private static Request ARequest(params (string Key, string Value)[] query) =>
        new(HttpMethod.Get, "/recipes/1/pdf", Empty, query.ToDictionary(q => q.Key, q => q.Value), Empty, Empty);

    private static Env BuildEnv() => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: Guest.Instance,
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository());

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
