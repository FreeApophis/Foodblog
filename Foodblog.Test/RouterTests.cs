namespace Foodblog.Test;

public class RouterTests
{
    [Fact]
    public async Task Get_htmx_min_js_returns_a_javascript_response()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/htmx.min.js", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(200, response.Status);
        Assert.StartsWith("application/javascript", response.ContentType);
        Assert.Contains("htmx", response.Body);
    }

    [Fact]
    public async Task Get_styles_css_returns_a_css_response()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/styles.css", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(200, response.Status);
        Assert.StartsWith("text/css", response.ContentType);
        Assert.Contains("--bg", response.Body);
    }

    [Fact]
    public async Task Get_articles_new_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/articles/new", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_articles_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/articles", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_recipes_returns_200_html_response()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/recipes", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(200, response.Status);
        Assert.StartsWith("text/html", response.ContentType);
    }

    [Fact]
    public async Task Get_recipe_by_id_returns_404_for_unknown_recipe()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/recipes/987654", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Get_recipes_new_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/recipes/new", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_recipes_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/recipes", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_recipes_form_ingredients_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/recipes/form/ingredients", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_recipes_form_steps_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/recipes/form/steps", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_recipe_edit_form_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/recipes/1/edit", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_recipe_update_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/recipes/1", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_article_edit_form_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/articles/1/edit", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_article_update_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/articles/1", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_delete_article_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/articles/1/delete", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_delete_recipe_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/recipes/1/delete", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_delete_ingredient_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/admin/ingredients/1/delete", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_admin_ingredients_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/admin/ingredients", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_admin_ingredients_new_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/admin/ingredients/new", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_admin_ingredients_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/admin/ingredients", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_admin_ingredient_edit_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/admin/ingredients/1/edit", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_admin_ingredient_update_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/admin/ingredients/1", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_delete_rule_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/admin/roles/1/rules/delete", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_images_library_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/images", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Post_images_upload_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Post, "/images", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_image_by_id_is_public_and_returns_404_for_unknown_image()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/images/987654", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Get_pages_list_is_public_and_returns_200()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/pages", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(200, response.Status);
        Assert.StartsWith("text/html", response.ContentType);
    }

    [Fact]
    public async Task Get_pages_new_redirects_guest_to_login()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/pages/new", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Equal("/login", response.Headers["Location"]);
    }

    [Fact]
    public async Task Get_page_by_id_returns_404_for_unknown_page()
    {
        var app = Router.Create(Routes.Build())(NotFoundTerminal);
        var env = BuildEnv();
        var request = new Request(HttpMethod.Get, "/pages/987654", Empty, Empty, Empty, Empty);

        var response = await app(request)(env);

        Assert.Equal(404, response.Status);
    }

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

    private static readonly App NotFoundTerminal = _ => _ => ValueTask.FromResult(Response.NotFound());
    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
