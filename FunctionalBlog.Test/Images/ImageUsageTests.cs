namespace FunctionalBlog.Test.Images;

public sealed class ImageUsageTests
{
    [Fact]
    public void ReferencedIds_collects_typed_and_inline_image_references()
    {
        var articles = new[]
        {
            Article.Create(
                new ArticleId(1),
                new ArticleTitle("Artikel"),
                new ArticleTeaser("Teaser mit /images/10"),
                new ArticleText("Text [img]/images/2[/img]"),
                new UserId(1),
                DateTimeOffset.UtcNow,
                Option.Some(new ImageId(1))),
        };

        var users = new[]
        {
            User.Create(new UserId(1), new Email("a@b.de"), new DisplayName("A"), "hash", ["Admin"], DateTimeOffset.UtcNow, Option.Some(new ImageId(3))),
        };

        var pages = new[]
        {
            Page.Create(new PageId(1), new PageTitle("Impressum"), new PageContent("Siehe [img]/images/6[/img]")),
        };

        var recipes = new[]
        {
            Recipe.Create(
                new RecipeId(1),
                new RecipeName("Rezept"),
                new RecipeDescription("Beschreibung [img]/images/7[/img]"),
                [new PreparationStep(1, "Schritt /images/8")],
                new UserId(1),
                Difficulty.Easy,
                [],
                2,
                [],
                ["/images/4"],
                [new RecipeHint("Tipp /images/9")]),
        };

        var ingredients = new[]
        {
            Ingredient.Create(
                new IngredientId(1),
                new IngredientName("Zutat"),
                image: "/images/5",
                description: "Beschreibung /images/11",
                density: 1m,
                pieceCount: 0m,
                calorificValue: 0m,
                protein: 0m,
                fat: 0m,
                carbohydrates: 0m,
                sugar: 0m,
                fiber: 0m),
        };

        var referenced = ImageUsage.ReferencedIds(articles, pages, recipes, ingredients, users);

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, referenced.OrderBy(i => i));
    }

    [Fact]
    public void Orphans_returns_only_library_images_that_are_not_referenced()
    {
        var library = new[] { Summary(1), Summary(2), Summary(99) };
        var referenced = new HashSet<int> { 1, 2 };

        var orphans = ImageUsage.Orphans(library, referenced);

        Assert.Equal(new[] { 99 }, orphans.Select(o => o.Id.Value));
    }

    private static ImageSummary Summary(int id) =>
        new(new ImageId(id), $"image-{id}.jpg", ImageContentType.Jpeg, 1234, DateTimeOffset.UtcNow, FocalPoint.Center);
}
