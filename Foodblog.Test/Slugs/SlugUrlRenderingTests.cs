namespace Foodblog.Test.Slugs;

// End-to-end proof that the rendering layer builds canonical /type/{slug} URLs when a SlugIndex is
// threaded through the ViewContext, and falls back to numeric ids when it is not.
public class SlugUrlRenderingTests
{
    private static readonly IReadOnlyDictionary<UserId, string> Names = new Dictionary<UserId, string> { [new UserId(1)] = "Anna" };
    private static readonly IReadOnlyDictionary<UserId, Option<ImageId>> Avatars = new Dictionary<UserId, Option<ImageId>> { [new UserId(1)] = Option<ImageId>.None };

    private static ViewContext WithSlugs => new(
        Guest.Instance,
        Functional.Identity,
        string.Empty,
        "light",
        Languages.Default,
        new SlugIndex(new Dictionary<string, IReadOnlyDictionary<int, string>>
        {
            [SlugEntityType.Article] = new Dictionary<int, string> { [1] = "mein-artikel" },
            [SlugEntityType.Recipe] = new Dictionary<int, string> { [1] = "mein-rezept" },
            [SlugEntityType.Page] = new Dictionary<int, string> { [1] = "impressum" },
            [SlugEntityType.Ingredient] = new Dictionary<int, string> { [1] = "mehl" },
        }));

    [Fact]
    public void Article_card_links_to_the_slug()
    {
        var html = BlogViews.Card(SampleArticle(), Names, Avatars, WithSlugs);

        Assert.Contains("href=\"/articles/mein-artikel\"", html);
        Assert.DoesNotContain("/articles/1", html);
    }

    [Fact]
    public void Recipe_card_links_to_the_slug()
    {
        var html = RecipeViews.Card(SampleRecipe(), Names, Avatars, WithSlugs);

        Assert.Contains("href=\"/recipes/mein-rezept\"", html);
        Assert.DoesNotContain("/recipes/1", html);
    }

    [Fact]
    public void Page_list_links_to_the_slug()
    {
        var html = PageViews.Index([SamplePage()], WithSlugs);

        Assert.Contains("href=\"/pages/impressum\"", html);
    }

    [Fact]
    public void Ingredient_tile_links_to_the_slug()
    {
        var page = Pagination.Paginate(new List<Ingredient> { SampleIngredient() }, 1);

        var html = IngredientViews.Index(page, WithSlugs);

        Assert.Contains("href=\"/ingredients/mehl\"", html);
    }

    [Fact]
    public void Card_falls_back_to_the_id_without_a_slug_index()
    {
        var html = BlogViews.Card(SampleArticle(), Names, Avatars, ViewContext.ForGuest());

        Assert.Contains("href=\"/articles/1\"", html);
    }

    private static Article SampleArticle() => Article.Create(
        new ArticleId(1),
        new ArticleTitle("Mein Artikel"),
        new ArticleTeaser("Ein ausreichend langer Teaser."),
        new ArticleText("Ein ausreichend langer Text."),
        new UserId(1),
        DateTimeOffset.UnixEpoch,
        Option<ImageId>.None);

    private static Recipe SampleRecipe() => Recipe.Create(
        new RecipeId(1),
        new RecipeName("Mein Rezept"),
        new RecipeDescription(string.Empty),
        [],
        new UserId(1),
        Difficulty.Easy,
        [],
        portions: 1,
        [],
        [],
        []);

    private static Page SamplePage() => Page.Create(new PageId(1), new PageTitle("Impressum"), new PageContent("Inhalt."));

    private static Ingredient SampleIngredient() => Ingredient.Create(
        new IngredientId(1),
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
}
