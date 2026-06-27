namespace FunctionalBlog.Test.Recipes;

public sealed class RecipeHandlerTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public async Task CreateRecipe_uploads_images_to_the_library_and_links_their_urls()
    {
        var env = BuildEnv();
        var request = ARecipeRequest("/recipes") with
        {
            Files =
            [
                new UploadedFile("images", "a.png", "image/png", PngBytes),
                new UploadedFile("images", "b.jpg", "image/jpeg", JpegBytes),
            ],
        };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        var recipe = Assert.Single(await env.Recipes.All());
        Assert.Equal(2, recipe.Images.Count);
        Assert.All(recipe.Images, url => Assert.StartsWith("/images/", url));
        Assert.Equal(2, (await env.Images.List()).Count);
    }

    [Fact]
    public async Task CreateRecipe_persists_preparation_and_cooking_time()
    {
        var env = BuildEnv();
        var request = ARecipeRequest("/recipes") with
        {
            Form = RecipeForm(("preparation_time", "10"), ("cooking_time", "20")),
        };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        var recipe = Assert.Single(await env.Recipes.All());
        Assert.Equal(10, recipe.PreparationTime);
        Assert.Equal(20, recipe.CookingTime);
    }

    [Fact]
    public async Task CreateRecipe_computes_calorific_value_per_serving_from_ingredients()
    {
        var env = BuildEnv();
        await SeedIngredient(env, "Mehl", calorificValue: 1464m); // ~350 kcal/100 g as kJ
        var request = ARecipeRequest("/recipes") with
        {
            Form = RecipeForm(
                ("portions", "2"),
                ("ingredient_name_0", "Mehl"),
                ("ingredient_amount_0", "200"),
                ("ingredient_unit_0", "1")),
        };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        var recipe = Assert.Single(await env.Recipes.All());

        // 200 g Mehl @ 1464 kJ/100 g = 2928 kJ ≈ 700 kcal total, over 2 servings = 350 kcal/serving.
        Assert.Equal(350, recipe.CalorificValue);
    }

    [Fact]
    public async Task ShowRecipe_renders_the_stat_strip_with_times_and_calories()
    {
        var env = BuildEnv();
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein klassischer Rührkuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            4,
            [],
            [],
            [],
            preparationTime: 10,
            cookingTime: 20,
            calorificValue: 227));

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("recipe-stats", response.Body);
        Assert.Contains("227", response.Body);
        Assert.Contains("10", response.Body);
        Assert.Contains("20", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_emits_recipe_json_ld_and_open_graph_tags()
    {
        var env = BuildEnv();
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein klassischer Rührkuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            4,
            [],
            ["/images/5"],
            [],
            preparationTime: 10,
            cookingTime: 20,
            calorificValue: 227));

        var request = AnEmptyRequest() with { BaseUrl = "https://foodblog.ch" };
        var response = await RecipeHandlers.ShowRecipe(id)(request)(env);

        Assert.Contains("application/ld+json", response.Body);
        Assert.Contains("\"@type\":\"Recipe\"", response.Body);
        Assert.Contains($"https://foodblog.ch/recipes/{id.Value}", response.Body);
        Assert.Contains("<meta property=\"og:image\" content=\"https://foodblog.ch/images/5\" />", response.Body);
        Assert.Contains("<meta property=\"og:type\" content=\"article\" />", response.Body);
    }

    [Fact]
    public async Task CreateRecipe_rejects_an_invalid_image_and_saves_nothing()
    {
        var env = BuildEnv();
        var request = ARecipeRequest("/recipes") with
        {
            Files = [new UploadedFile("images", "bad.exe", "image/png", [0x4D, 0x5A, 0x90])],
        };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(400, response.Status);
        Assert.Empty(await env.Recipes.All());
        Assert.Empty(await env.Images.List());
    }

    [Fact]
    public async Task UpdateRecipe_keeps_unremoved_images_and_appends_newly_uploaded_ones()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, ["/images/1"]);
        var form = RecipeForm(("existing_image_0", "/images/1"));
        var request = new Request(HttpMethod.Post, $"/recipes/{id.Value}", Empty, Empty, form, Empty)
        {
            Files = [new UploadedFile("images", "new.png", "image/png", PngBytes)],
        };

        var response = await RecipeHandlers.UpdateRecipe(id)(request)(env);

        Assert.Equal(303, response.Status);
        var recipe = FunctionalAssert.Some(await env.Recipes.Find(id));
        Assert.Equal(2, recipe.Images.Count);
        Assert.Equal("/images/1", recipe.Images[0]);
        Assert.StartsWith("/images/", recipe.Images[1]);
    }

    [Fact]
    public async Task UpdateRecipe_drops_existing_images_marked_for_removal()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, ["/images/1"]);
        var form = RecipeForm(("existing_image_0", "/images/1"), ("remove_image_0", "on"));
        var request = new Request(HttpMethod.Post, $"/recipes/{id.Value}", Empty, Empty, form, Empty);

        var response = await RecipeHandlers.UpdateRecipe(id)(request)(env);

        Assert.Equal(303, response.Status);
        var recipe = FunctionalAssert.Some(await env.Recipes.Find(id));
        Assert.Empty(recipe.Images);
    }

    [Fact]
    public async Task ShowRecipe_applies_the_focal_point_to_the_header_slider_image()
    {
        var env = BuildEnv() with
        {
            Focal = new FocalIndex(new Dictionary<int, FocalPoint> { [7] = new FocalPoint(0.2, 0.8) }),
        };
        var id = await SeedRecipe(env, ["/images/7"]);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("object-position:20% 80%", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_renders_a_placeholder_when_there_are_no_images()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("recipe-image-placeholder", response.Body);
        Assert.DoesNotContain("slider-fade", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_renders_an_autoplaying_crossfade_slider_with_every_image()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, ["/images/3", "/images/4"]);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("slider-fade", response.Body);
        Assert.Contains("/images/3", response.Body);
        Assert.Contains("/images/4", response.Body);

        // Pure-CSS autoplay: a generated keyframe animation that loops without JS.
        Assert.Contains("@keyframes", response.Body);
        Assert.Contains("infinite", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_slider_offers_manual_selection_controls()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, ["/images/3", "/images/4"]);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        // A hidden radio per image plus a clickable dot label lets the visitor take over from autoplay.
        Assert.Contains("type=\"radio\"", response.Body);
        Assert.Contains("class=\"slider-pick\"", response.Body);
        Assert.Contains($"<label class=\"slider-dot\" for=\"recipe-{id.Value}-pick-0\"", response.Body);
        Assert.Contains($"<label class=\"slider-dot\" for=\"recipe-{id.Value}-pick-1\"", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_shows_a_single_image_without_autoplay()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, ["/images/3"]);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("/images/3", response.Body);
        Assert.DoesNotContain("@keyframes", response.Body);
    }

    [Fact]
    public async Task CreateRecipe_with_a_known_ingredient_name_reuses_the_existing_ingredient()
    {
        var env = BuildEnv();
        var mehl = await SeedIngredient(env, "Mehl");
        var request = ARecipeRequest("/recipes") with { Form = RecipeForm(IngredientFields("Mehl", "200", "1")) };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        var recipe = Assert.Single(await env.Recipes.All());
        Assert.Equal(mehl.Id, Assert.Single(recipe.Ingredients).IngredientId);
        Assert.Single(await env.Ingredients.All());
    }

    [Fact]
    public async Task CreateRecipe_with_an_unknown_ingredient_name_creates_a_new_ingredient()
    {
        var env = BuildEnv();
        var request = ARecipeRequest("/recipes") with { Form = RecipeForm(IngredientFields("Dinkelmehl", "200", "1")) };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        var created = Assert.Single(await env.Ingredients.All());
        Assert.Equal("Dinkelmehl", created.Name.Value);
        var recipe = Assert.Single(await env.Recipes.All());
        Assert.Equal(created.Id, Assert.Single(recipe.Ingredients).IngredientId);
    }

    // A quick-created ingredient must get a slug registered immediately, like the admin create path
    // does — otherwise its /ingredients/{slug} link 404s until the startup SlugBackfill fills it in.
    [Fact]
    public async Task CreateRecipe_with_an_unknown_ingredient_name_registers_its_slug()
    {
        var slugs = new InMemorySlugRepository();
        var env = BuildEnv(slugs: slugs);
        var request = ARecipeRequest("/recipes") with { Form = RecipeForm(IngredientFields("Linguine", "200", "1")) };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        var created = Assert.Single(await env.Ingredients.All());
        var slug = FunctionalAssert.Some(await slugs.FindSlug(SlugEntityType.Ingredient, created.Id.Value));
        Assert.Equal("linguine", slug);
    }

    [Fact]
    public async Task CreateRecipe_succeeds_when_existing_ingredients_share_a_name()
    {
        var env = BuildEnv();
        await SeedIngredient(env, "Apfel");
        await SeedIngredient(env, "apfel");
        var request = ARecipeRequest("/recipes") with { Form = RecipeForm(IngredientFields("Koriander", "20", "1")) };

        var response = await RecipeHandlers.CreateRecipe(request)(env);

        Assert.Equal(303, response.Status);
        Assert.Single(await env.Recipes.All());
    }

    [Fact]
    public async Task Index_shows_at_most_twelve_cards_on_the_first_page()
    {
        var env = BuildEnv();
        await SeedRecipes(env, 13);

        var response = await RecipeHandlers.Index(ARequest("/recipes"))(env);

        Assert.Equal(12, CountCards(response.Body));
        Assert.Contains("/recipes?page=2", response.Body);
    }

    [Fact]
    public async Task Index_shows_the_remaining_cards_on_the_second_page()
    {
        var env = BuildEnv();
        await SeedRecipes(env, 13);

        var response = await RecipeHandlers.Index(ARequest("/recipes", ("page", "2")))(env);

        Assert.Equal(1, CountCards(response.Body));
    }

    [Fact]
    public async Task Index_omits_pagination_when_everything_fits_on_one_page()
    {
        var env = BuildEnv();
        await SeedRecipes(env, 5);

        var response = await RecipeHandlers.Index(ARequest("/recipes"))(env);

        Assert.Equal(5, CountCards(response.Body));
        Assert.DoesNotContain("class=\"pagination\"", response.Body);
    }

    [Fact]
    public async Task Index_clamps_an_out_of_range_page_to_the_last_page()
    {
        var env = BuildEnv();
        await SeedRecipes(env, 13);

        var response = await RecipeHandlers.Index(ARequest("/recipes", ("page", "99")))(env);

        Assert.Equal(1, CountCards(response.Body));
    }

    [Fact]
    public async Task ShowRecipe_scales_ingredient_amounts_to_the_requested_portions_but_keeps_per_serving_calories()
    {
        var env = BuildEnv();
        var mehl = await SeedIngredient(env, "Mehl");
        var id = await SeedScalableRecipe(env, mehl.Id, baseAmount: 150m, basePortions: 2, calories: 200);

        var response = await RecipeHandlers.ShowRecipe(id)(ARequest($"/recipes/{id.Value}", ("portions", "4")))(env);

        Assert.Contains("300", response.Body);  // 150 g doubled for 4 instead of 2 portions
        Assert.Contains("200", response.Body);  // calories are per serving — unchanged by the portions
    }

    [Fact]
    public async Task ShowRecipe_renders_an_instructions_header_with_the_step_count()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);  // seeded with a single preparation step

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("recipe-section-head", response.Body);
        Assert.Contains("recipe.instructions", response.Body);
        Assert.Contains("1 recipe.steps", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_lists_per_ingredient_calories()
    {
        var env = BuildEnv();
        var mehl = await SeedIngredient(env, "Mehl", calorificValue: 1464m); // ~350 kcal/100 g as kJ
        var id = await SeedScalableRecipe(env, mehl.Id, baseAmount: 200m, basePortions: 2, calories: 350);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("ingredient-kcal", response.Body);
        Assert.Contains("700", response.Body);  // 200 g Mehl @ 1464 kJ/100 g = 2928 kJ ≈ 700 kcal for this line
    }

    [Fact]
    public async Task ShowRecipe_links_each_ingredient_name_to_its_page()
    {
        var env = BuildEnv();
        var mehl = await SeedIngredient(env, "Mehl");
        var id = await SeedScalableRecipe(env, mehl.Id, baseAmount: 200m, basePortions: 2, calories: 350);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains($"""<span class="name"><a href="/ingredients/{mehl.Id.Value}">Mehl</a></span>""", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_without_a_portions_query_shows_the_base_amounts()
    {
        var env = BuildEnv();
        var mehl = await SeedIngredient(env, "Mehl");
        var id = await SeedScalableRecipe(env, mehl.Id, baseAmount: 150m, basePortions: 2, calories: 200);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("150", response.Body);
        Assert.Contains("200", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_renders_a_portions_menu_with_preset_options()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("portions-menu", response.Body);
        Assert.Contains($"/recipes/{id.Value}?portions=12", response.Body);
        Assert.Contains($"/recipes/{id.Value}?portions=100", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_renders_edit_and_delete_as_icon_buttons_in_the_meta_row()
    {
        var env = BuildEnv(EditorUser());
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("icon-round", response.Body);
        Assert.Contains($"/recipes/{id.Value}/edit", response.Body);
        Assert.Contains($"/recipes/{id.Value}/delete", response.Body);
        Assert.DoesNotContain("recipe-actions", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_renders_a_pdf_split_button_offering_both_designs()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("pdf-split", response.Body);
        Assert.Contains($"href=\"/recipes/{id.Value}/pdf\"", response.Body);
        Assert.Contains($"href=\"/recipes/{id.Value}/pdf?design=alternative\"", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_pdf_button_carries_the_selected_portion_count()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.ShowRecipe(id)(ARequest($"/recipes/{id.Value}", ("portions", "8")))(env);

        Assert.Contains($"href=\"/recipes/{id.Value}/pdf?portions=8\"", response.Body);
        Assert.Contains($"href=\"/recipes/{id.Value}/pdf?design=alternative&portions=8\"", response.Body);
    }

    [Fact]
    public async Task ShowRecipe_renders_a_breadcrumb_instead_of_a_back_link()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.ShowRecipe(id)(AnEmptyRequest())(env);

        Assert.Contains("class=\"breadcrumb\"", response.Body);
        Assert.Contains("class=\"crumb-current\"", response.Body);
        Assert.DoesNotContain("common.back", response.Body);
    }

    [Fact]
    public async Task NewRecipeForm_breadcrumb_ends_in_the_new_leaf()
    {
        var env = BuildEnv();

        var response = await RecipeHandlers.NewRecipeForm(AnEmptyRequest())(env);

        Assert.Contains("class=\"breadcrumb\"", response.Body);
        Assert.Contains("common.new", response.Body);
        Assert.DoesNotContain("common.back", response.Body);
    }

    [Fact]
    public async Task EditRecipeForm_breadcrumb_links_the_recipe_name_back_to_its_detail_page()
    {
        var env = BuildEnv();
        var id = await SeedRecipe(env, []);

        var response = await RecipeHandlers.EditRecipeForm(id)(AnEmptyRequest())(env);

        Assert.Contains("class=\"breadcrumb\"", response.Body);
        Assert.Contains($"href=\"/recipes/{id.Value}\"", response.Body);
        Assert.Contains("common.edit", response.Body);
        Assert.DoesNotContain("common.back", response.Body);
    }

    [Fact]
    public async Task IngredientSearch_returns_only_matching_ingredient_names()
    {
        var env = BuildEnv();
        await SeedIngredient(env, "Mehl");
        await SeedIngredient(env, "Zucker");
        var form = new Dictionary<string, string> { ["index"] = "0", ["ingredient_name_0"] = "Me" };
        var request = new Request(HttpMethod.Post, "/recipes/form/ingredient-search", Empty, Empty, form, Empty);

        var response = await RecipeHandlers.IngredientSearch(request)(env);

        Assert.Contains("Mehl", response.Body);
        Assert.DoesNotContain("Zucker", response.Body);
    }

    [Fact]
    public async Task IngredientSelect_renders_the_combobox_input_with_the_chosen_name()
    {
        var env = BuildEnv();
        var form = new Dictionary<string, string> { ["index"] = "0", ["selected"] = "Mehl" };
        var request = new Request(HttpMethod.Post, "/recipes/form/ingredient-select", Empty, Empty, form, Empty);

        var response = await RecipeHandlers.IngredientSelect(request)(env);

        Assert.Contains("ingredient_name_0", response.Body);
        Assert.Contains("value=\"Mehl\"", response.Body);
    }

    // The recipe title field is named "name"; since the match button lives inside the same
    // form, htmx serialises the whole form on click. The button must therefore post the chosen
    // ingredient under a distinct key, or the two "name" values collide into "<title>,<chosen>".
    [Fact]
    public async Task IngredientMatches_button_does_not_post_under_the_colliding_name_key()
    {
        var env = BuildEnv();
        var mehl = await SeedIngredient(env, "Mehl");

        var html = RecipeViews.IngredientMatches("0", "Me", [mehl], env.T);

        Assert.DoesNotContain("name=\"name\"", html);
        Assert.Contains("name=\"selected\" value=\"Mehl\"", html);
    }

    // Adding a row swaps the whole section, replacing the button that had focus. The new row's
    // first field carries `autofocus` so the script can move the cursor there for the next entry.
    [Fact]
    public async Task IngredientsSection_add_autofocuses_the_new_rows_first_field()
    {
        var env = BuildEnv();
        var form = new Dictionary<string, string>
        {
            ["action"] = "add-ingredient",
            ["ingredient_name_0"] = "Mehl",
            ["ingredient_amount_0"] = "200",
            ["ingredient_unit_0"] = "1",
        };
        var request = new Request(HttpMethod.Post, "/recipes/form/ingredients", Empty, Empty, form, Empty);

        var response = await RecipeHandlers.IngredientsSection(request)(env);

        Assert.Contains("ingredient_amount_1", response.Body);
        Assert.Contains("value=\"\" autofocus", response.Body);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(response.Body, "autofocus"));
    }

    [Fact]
    public async Task IngredientsSection_remove_does_not_autofocus_any_field()
    {
        var env = BuildEnv();
        var form = new Dictionary<string, string>
        {
            ["action"] = "remove-ingredient-0",
            ["ingredient_name_0"] = "Mehl",
            ["ingredient_amount_0"] = "200",
            ["ingredient_unit_0"] = "1",
        };
        var request = new Request(HttpMethod.Post, "/recipes/form/ingredients", Empty, Empty, form, Empty);

        var response = await RecipeHandlers.IngredientsSection(request)(env);

        Assert.DoesNotContain("autofocus", response.Body);
    }

    [Fact]
    public async Task StepsSection_add_autofocuses_the_new_step_textarea()
    {
        var env = BuildEnv();
        var form = new Dictionary<string, string>
        {
            ["action"] = "add-step",
            ["step_0"] = "Erster Schritt.",
        };
        var request = new Request(HttpMethod.Post, "/recipes/form/steps", Empty, Empty, form, Empty);

        var response = await RecipeHandlers.StepsSection(request)(env);

        Assert.Contains("name=\"step_1\" rows=\"3\" autofocus", response.Body);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(response.Body, "autofocus"));
    }

    private static async Task<Ingredient> SeedIngredient(
        Env env, string name, decimal calorificValue = 0m, decimal density = 1m, decimal pieceCount = 0m)
    {
        var ingredient = Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName(name),
            image: string.Empty,
            description: string.Empty,
            density: density,
            pieceCount: pieceCount,
            calorificValue: calorificValue,
            protein: 0,
            fat: 0,
            carbohydrates: 0,
            sugar: 0,
            fiber: 0);
        await env.Ingredients.Save(ingredient);
        return ingredient;
    }

    private static int CountCards(string body)
    {
        var count = 0;
        var index = 0;
        const string marker = "class=\"recipe-card\"";
        while ((index = body.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }

        return count;
    }

    private static async Task SeedRecipes(Env env, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var id = await env.Recipes.NextId();
            await env.Recipes.Save(Recipe.Create(
                id,
                new RecipeName($"Rezept {i:D2}"),
                new RecipeDescription("Beschreibung."),
                [new PreparationStep(1, "Schritt.")],
                new UserId(1),
                Difficulty.Easy,
                [],
                4,
                [],
                [],
                []));
        }
    }

    private static (string, string)[] IngredientFields(string name, string amount, string unit) =>
        [("ingredient_name_0", name), ("ingredient_amount_0", amount), ("ingredient_unit_0", unit)];

    private static async Task<RecipeId> SeedScalableRecipe(Env env, IngredientId ingredientId, decimal baseAmount, int basePortions, int calories)
    {
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein klassischer Rührkuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            basePortions,
            [new RecipeIngredient(ingredientId, baseAmount, Gram)],
            [],
            [],
            calorificValue: calories));
        return id;
    }

    private static async Task<RecipeId> SeedRecipe(Env env, IReadOnlyList<string> images)
    {
        var id = await env.Recipes.NextId();
        await env.Recipes.Save(Recipe.Create(
            id,
            new RecipeName("Rührkuchen"),
            new RecipeDescription("Ein klassischer Rührkuchen."),
            [new PreparationStep(1, "Alles verrühren.")],
            new UserId(1),
            Difficulty.Easy,
            [],
            4,
            [],
            images,
            []));
        return id;
    }

    private static Request ARecipeRequest(string path) =>
        new(HttpMethod.Post, path, Empty, Empty, RecipeForm(), Empty);

    private static Dictionary<string, string> RecipeForm(params (string Key, string Value)[] extra)
    {
        var form = new Dictionary<string, string>
        {
            ["name"] = "Rührkuchen",
            ["description"] = "Ein klassischer Rührkuchen.",
            ["portions"] = "4",
            ["difficulty"] = "0",
            ["step_0"] = "Alles verrühren.",
        };

        foreach (var (key, value) in extra)
        {
            form[key] = value;
        }

        return form;
    }

    private static Request AnEmptyRequest() =>
        new(HttpMethod.Get, "/", Empty, Empty, Empty, Empty);

    private static Request ARequest(string path, params (string Key, string Value)[] query) =>
        new(HttpMethod.Get, path, Empty, query.ToDictionary(q => q.Key, q => q.Value), Empty, Empty);

    private static AuthenticatedUser AuthUser()
    {
        var user = User.Create(
            new UserId(1),
            new Email("admin@blog.de"),
            new DisplayName("Admin"),
            "hash",
            [],
            DateTimeOffset.UtcNow);
        return new AuthenticatedUser(user, []);
    }

    private static AuthenticatedUser EditorUser()
    {
        var user = User.Create(
            new UserId(1),
            new Email("admin@blog.de"),
            new DisplayName("Admin"),
            "hash",
            ["Editor"],
            DateTimeOffset.UtcNow);
        var role = Role.Create(new RoleId(1), "Editor")
            .AddRule(new PermissionRule("Edit", "recipe"))
            .AddRule(new PermissionRule("Delete", "recipe"));
        return new AuthenticatedUser(user, [role]);
    }

    private static Env BuildEnv(IPrincipal? currentUser = null, ISlugRepository? slugs = null) => new(
        Articles: new InMemoryArticleRepository(),
        Users: new InMemoryUserRepository(),
        Roles: new InMemoryRoleRepository(),
        Sessions: new InMemorySessionStore(),
        PasswordResets: new InMemoryPasswordResetTokenStore(),
        PasswordHasher: new Pbkdf2PasswordHasher(),
        Clock: new SystemClock(),
        Log: new ConsoleLog(),
        CurrentUser: currentUser ?? AuthUser(),
        Recipes: new InMemoryRecipeRepository(),
        Ingredients: new InMemoryIngredientRepository(),
        Units: new InMemoryUnitRepository(),
        Images: new InMemoryImageRepository(),
        Pages: new InMemoryPageRepository(),
        Slugs: slugs);

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
}
