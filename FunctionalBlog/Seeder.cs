using System.Security.Cryptography;

namespace FunctionalBlog;

public static class Seeder
{
    private const string DefaultAdminEmail = "admin@blog.de";

    // Earlier releases created DefaultAdminEmail with this password, and the 0002_seed migration still
    // ships it. It is public, so it is only ever used to detect (and lock) that account, never to log in.
    private const string LeakedDefaultPassword = "Admin1234";

    private const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int GeneratedPasswordLength = 24;
    private const string AdminRoleName = "Admin";
    private const string DefaultRoleName = "Benutzer";

    public static ValueTask SeedAsync(Env env) => SeedAsync(env, Option<string>.None);

    public static async ValueTask SeedAsync(Env env, Option<string> bootstrapAdminEmail)
    {
        await SeedRoles(env);
        await LockLeakedDefaultAdmin(env);
        var bootstrapAdmin = await BootstrapAdmin(env, bootstrapAdminEmail);
        await SeedSampleArticles(env, bootstrapAdmin);
        await SeedSampleIngredients(env);
        await SeedSampleRecipes(env, bootstrapAdmin);
        await RecomputeRecipeCalories(env);
        await SeedConfiguration(env);

        if (env.Translations is not null)
        {
            await TranslationSeeder.SeedAsync(env.Translations);
        }
    }

    // Writes each default configuration value once, only when the key is still absent, so a fresh
    // database gets the full set of keys (blank SMTP credentials, default site name) while existing
    // databases — and any admin edits — are left untouched.
    private static async ValueTask SeedConfiguration(Env env)
    {
        if (env.Configuration is null)
        {
            return;
        }

        foreach (var (key, value) in ConfigurationDefaults.Values)
        {
            if ((await env.Configuration.Get(key)) is [])
            {
                await env.Configuration.Set(key, value);
            }
        }
    }

    // Recipe calories are derived from the ingredients, not entered. This brings every stored recipe
    // in line with the current ingredient data on startup — fixing values created before calories
    // were computed, and keeping them fresh as ingredient nutrition is edited. Cheap and idempotent.
    private static async ValueTask RecomputeRecipeCalories(Env env)
    {
        var catalog = (await env.Ingredients.All()).ToDictionary(i => i.Id);

        foreach (var recipe in await env.Recipes.All())
        {
            var perServing = CalorieCalculator.PerServing(recipe.Ingredients, recipe.Portions, catalog);
            if (perServing != recipe.CalorificValue)
            {
                await env.Recipes.UpdateCalorificValue(recipe.Id, perServing);
            }
        }
    }

    private static readonly PermissionRule[] DefaultRoleRules =
    [
        new("View", "article"),
        new("View", "image"),
        new("View", "page"),
    ];

    private static readonly PermissionRule[] AdminRoleRules =
    [
        new("Manage", "article"),
        new("Manage", "user"),
        new("Manage", "role"),
        new("Manage", "rule"),
        new("Create", "article"),
        new("Edit", "article"),
        new("Delete", "article"),
        new("Create", "role"),
        new("View", "article"),
        new("Create", "recipe"),
        new("Edit", "recipe"),
        new("Delete", "recipe"),
        new("Manage", "recipe"),
        new("Create", "ingredient"),
        new("Edit", "ingredient"),
        new("Delete", "ingredient"),
        new("Manage", "ingredient"),
        new("Create", "unit"),
        new("Edit", "unit"),
        new("Delete", "unit"),
        new("Manage", "unit"),
        new("Create", "image"),
        new("Edit", "image"),
        new("Delete", "image"),
        new("Manage", "image"),
        new("View", "page"),
        new("Create", "page"),
        new("Edit", "page"),
        new("Delete", "page"),
        new("Manage", "page"),
        new("Manage", "search"),
        new("Manage", "settings"),
    ];

    private static async ValueTask SeedRoles(Env env)
    {
        await EnsureRole(env, DefaultRoleName, DefaultRoleRules);
        await EnsureRole(env, AdminRoleName, AdminRoleRules);
    }

    // Creates the role if missing and additively grants any required permissions it is
    // lacking, so existing databases pick up rules introduced by new features (e.g. images).
    private static async ValueTask EnsureRole(Env env, string name, IReadOnlyList<PermissionRule> requiredRules)
    {
        var found = await env.Roles.FindByName(name);
        var role = found is [var existing] ? existing : Role.Create(await env.Roles.NextId(), name);

        var missing = requiredRules.Where(rule => !role.Rules.Contains(rule)).ToList();
        if (found is [_] && missing.Count == 0)
        {
            return;
        }

        foreach (var rule in missing)
        {
            role = role.AddRule(rule);
        }

        await env.Roles.Save(role);
    }

    // If the account still has the publicly known default password, replace its hash with one of a
    // random password nobody knows and end its sessions. The account and its content stay; a real
    // admin can take it over from the admin area.
    private static async ValueTask LockLeakedDefaultAdmin(Env env)
    {
        if (Email.ParseOrNone(DefaultAdminEmail) is [var email] &&
            (await env.Users.FindByEmail(email)) is [var user] &&
            env.PasswordHasher.Verify(LeakedDefaultPassword, user.PasswordHash))
        {
            await env.Users.Save(user with { PasswordHash = env.PasswordHasher.Hash(GeneratePassword()) });
            await env.Sessions.DeleteAllForUser(user.Id);
            env.Log.Info($"WARNING: The account {email.Value} still had the publicly known default password. " +
                "Its password has been replaced with an unknown random one and its sessions were ended. " +
                "Use another admin account to take over its content or to give it a new password.");
        }
    }

    // Only a database without any users gets an admin, with a random password that is logged once.
    // Returns the admin only when it was created by this run, so sample content is attributed to it
    // on a fresh database and never added to an existing one.
    private static async ValueTask<Option<User>> BootstrapAdmin(Env env, Option<string> bootstrapAdminEmail)
    {
        if ((await env.Users.All()).Count > 0 ||
            bootstrapAdminEmail.SelectMany(Email.ParseOrNone).OrElse(Email.ParseOrNone(DefaultAdminEmail)) is not [var email])
        {
            return Option<User>.None;
        }

        var password = GeneratePassword();
        var user = User.Create(await env.Users.NextId(), email, new DisplayName("Admin"), env.PasswordHasher.Hash(password), [AdminRoleName], env.Clock.Now, emailVerified: true);
        await env.Users.Save(user);

        env.Log.Info($"WARNING: No users found. Created the admin account {email.Value} with " +
            $"bootstrap-admin-password: {password} " +
            "(shown only this once). Log in and change it immediately.");

        return Option.Some(user);
    }

    private static string GeneratePassword() =>
        RandomNumberGenerator.GetString(PasswordAlphabet, GeneratedPasswordLength);

    private static async ValueTask SeedSampleArticles(Env env, Option<User> bootstrapAdmin)
    {
        if ((await env.Articles.All()).Count > 0)
        {
            return;
        }

        if (bootstrapAdmin is [var admin])
        {
            var id1 = await env.Articles.NextId();
            await env.Articles.Save(Article.Create(
                id1,
                new ArticleTitle("Hallo funktionales Blog"),
                new ArticleTeaser("Ein funktionaler Ansatz für einen modernen Blog mit .NET 10."),
                new ArticleText("Dieser Blog wurde mit einem funktionalen Ansatz in .NET 10 entwickelt. " +
                    "Das Kernstück ist eine curried, reader-style Pipeline ausgedrückt mit Delegates."),
                admin.Id,
                new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero)));

            var id2 = await env.Articles.NextId();
            await env.Articles.Save(Article.Create(
                id2,
                new ArticleTitle("Macarons selbst backen"),
                new ArticleTeaser("Macarons sind kleine französische Mandelbaisers mit einer Cremefüllung."),
                new ArticleText("Macarons sind kleine französische Mandelbaisers mit einer Cremefüllung. " +
                    "Das Rezept ist anspruchsvoll, aber das Ergebnis ist köstlich."),
                admin.Id,
                new DateTimeOffset(2026, 2, 20, 14, 0, 0, TimeSpan.Zero)));
        }
    }

    private static async ValueTask SeedSampleIngredients(Env env)
    {
        if ((await env.Ingredients.All()).Count > 0)
        {
            return;
        }

        var mehlId = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            mehlId,
            new IngredientName("Mehl"),
            string.Empty,
            "Weizenmehl Type 405",
            density: 0.6m,
            pieceCount: 0m,
            calorificValue: 364m,
            protein: 10m,
            fat: 1.2m,
            carbohydrates: 76m,
            sugar: 0.3m,
            fiber: 2.7m));

        var zuckerId = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            zuckerId,
            new IngredientName("Zucker"),
            string.Empty,
            "Weißer Haushaltszucker",
            density: 1.6m,
            pieceCount: 0m,
            calorificValue: 400m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 100m,
            sugar: 100m,
            fiber: 0m));

        var butterId = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            butterId,
            new IngredientName("Butter"),
            string.Empty,
            "Süßrahmbutter",
            density: 0.96m,
            pieceCount: 0m,
            calorificValue: 740m,
            protein: 0.7m,
            fat: 83m,
            carbohydrates: 0.4m,
            sugar: 0.4m,
            fiber: 0m));

        var eierId = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            eierId,
            new IngredientName("Eier"),
            string.Empty,
            "Hühnereier (Größe M, ca. 60 g)",
            density: 1.0m,
            pieceCount: 60m,
            calorificValue: 155m,
            protein: 13m,
            fat: 11m,
            carbohydrates: 1m,
            sugar: 0.4m,
            fiber: 0m));

        var milchId = await env.Ingredients.NextId();
        await env.Ingredients.Save(Ingredient.Create(
            milchId,
            new IngredientName("Milch"),
            string.Empty,
            "Vollmilch 3,5 % Fett",
            density: 1.03m,
            pieceCount: 0m,
            calorificValue: 61m,
            protein: 3.4m,
            fat: 3.4m,
            carbohydrates: 4.7m,
            sugar: 4.7m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Zwiebel"),
            string.Empty,
            "Gemüsezwiebel, mittelgroß (ca. 150 g)",
            density: 0.75m,
            pieceCount: 150m,
            calorificValue: 40m,
            protein: 1.1m,
            fat: 0.1m,
            carbohydrates: 9.3m,
            sugar: 4.2m,
            fiber: 1.7m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Öl"),
            string.Empty,
            "Neutrales Pflanzenöl",
            density: 0.92m,
            pieceCount: 0m,
            calorificValue: 900m,
            protein: 0m,
            fat: 100m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Paprikapulver"),
            string.Empty,
            "Edelsüßes Paprikapulver",
            density: 0.5m,
            pieceCount: 0m,
            calorificValue: 282m,
            protein: 14m,
            fat: 13m,
            carbohydrates: 54m,
            sugar: 10m,
            fiber: 34m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Kartoffeln"),
            string.Empty,
            "Festkochende Kartoffeln",
            density: 1.1m,
            pieceCount: 0m,
            calorificValue: 77m,
            protein: 2m,
            fat: 0.1m,
            carbohydrates: 17m,
            sugar: 0.8m,
            fiber: 2.2m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Knoblauch"),
            string.Empty,
            "Knoblauchzehe (ca. 5 g)",
            density: 0.95m,
            pieceCount: 5m,
            calorificValue: 149m,
            protein: 6.4m,
            fat: 0.5m,
            carbohydrates: 33m,
            sugar: 1m,
            fiber: 2.1m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Kümmel"),
            string.Empty,
            "Ganze Kümmelsamen",
            density: 0.5m,
            pieceCount: 0m,
            calorificValue: 333m,
            protein: 17m,
            fat: 14.6m,
            carbohydrates: 50m,
            sugar: 1.5m,
            fiber: 10m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Karotten"),
            string.Empty,
            "Möhren, mittelgroß (ca. 100 g)",
            density: 1.0m,
            pieceCount: 100m,
            calorificValue: 41m,
            protein: 0.9m,
            fat: 0.2m,
            carbohydrates: 10m,
            sugar: 4.7m,
            fiber: 2.8m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Gewürzgurken"),
            string.Empty,
            "Eingelegte Gewürzgurken (ca. 50 g pro Stück)",
            density: 1.0m,
            pieceCount: 50m,
            calorificValue: 11m,
            protein: 0.6m,
            fat: 0.1m,
            carbohydrates: 2.4m,
            sugar: 1.1m,
            fiber: 0.5m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Salz"),
            string.Empty,
            "Speisesalz",
            density: 2.16m,
            pieceCount: 0m,
            calorificValue: 0m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 0m,
            sugar: 0m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Gurkenwasser"),
            string.Empty,
            "Sud aus dem Gewürzgurkenglas",
            density: 1.01m,
            pieceCount: 0m,
            calorificValue: 5m,
            protein: 0m,
            fat: 0m,
            carbohydrates: 1m,
            sugar: 0.5m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Älplermagronen"),
            string.Empty,
            "Kurze Alpennudeln (Makkaroni)",
            density: 0.65m,
            pieceCount: 0m,
            calorificValue: 365m,
            protein: 12m,
            fat: 1.5m,
            carbohydrates: 73m,
            sugar: 2m,
            fiber: 3m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Äpfel"),
            string.Empty,
            "Säuerliche Äpfel, z. B. Boskoop (ca. 150 g pro Stück)",
            density: 0.85m,
            pieceCount: 150m,
            calorificValue: 52m,
            protein: 0.3m,
            fat: 0.2m,
            carbohydrates: 14m,
            sugar: 10m,
            fiber: 2.4m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Speckwürfel"),
            string.Empty,
            "Geräucherte Speckwürfel",
            density: 0.95m,
            pieceCount: 0m,
            calorificValue: 400m,
            protein: 16m,
            fat: 37m,
            carbohydrates: 0.5m,
            sugar: 0m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Frühlingszwiebeln"),
            string.Empty,
            "Frische Frühlingszwiebeln (Bund)",
            density: 0.95m,
            pieceCount: 0m,
            calorificValue: 32m,
            protein: 1.8m,
            fat: 0.2m,
            carbohydrates: 7m,
            sugar: 2.3m,
            fiber: 2.6m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Rahm"),
            string.Empty,
            "Vollrahm 35 % Fett",
            density: 0.99m,
            pieceCount: 0m,
            calorificValue: 340m,
            protein: 2.5m,
            fat: 35m,
            carbohydrates: 3m,
            sugar: 3m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Gemüsebouillon"),
            string.Empty,
            "Klare Gemüsebrühe",
            density: 1.01m,
            pieceCount: 0m,
            calorificValue: 10m,
            protein: 0.3m,
            fat: 0.2m,
            carbohydrates: 1.5m,
            sugar: 0.5m,
            fiber: 0m));

        await env.Ingredients.Save(Ingredient.Create(
            await env.Ingredients.NextId(),
            new IngredientName("Bergkäse"),
            string.Empty,
            "Würziger Schweizer Bergkäse, gerieben",
            density: 1.1m,
            pieceCount: 0m,
            calorificValue: 380m,
            protein: 27m,
            fat: 30m,
            carbohydrates: 0.5m,
            sugar: 0m,
            fiber: 0m));
    }

    private static async ValueTask SeedSampleRecipes(Env env, Option<User> bootstrapAdmin)
    {
        if ((await env.Recipes.All()).Count > 0)
        {
            return;
        }

        if (bootstrapAdmin is [var admin])
        {
            var ingredients = (await env.Ingredients.All()).ToDictionary(i => i.Name.Value, i => i.Id);

            if (!ingredients.TryGetValue("Mehl", out var mehlId) ||
                !ingredients.TryGetValue("Zucker", out var zuckerId) ||
                !ingredients.TryGetValue("Butter", out var butterId) ||
                !ingredients.TryGetValue("Eier", out var eierId) ||
                !ingredients.TryGetValue("Milch", out var milchId) ||
                !ingredients.TryGetValue("Zwiebel", out var zwiebelId) ||
                !ingredients.TryGetValue("Öl", out var ölId) ||
                !ingredients.TryGetValue("Paprikapulver", out var paprikaId) ||
                !ingredients.TryGetValue("Kartoffeln", out var kartoffelnId) ||
                !ingredients.TryGetValue("Knoblauch", out var knoblauchId) ||
                !ingredients.TryGetValue("Kümmel", out var kümmelId) ||
                !ingredients.TryGetValue("Karotten", out var karottenId) ||
                !ingredients.TryGetValue("Gewürzgurken", out var gewürzgurkenId) ||
                !ingredients.TryGetValue("Salz", out var salzId) ||
                !ingredients.TryGetValue("Gurkenwasser", out var gurkenwasserId) ||
                !ingredients.TryGetValue("Älplermagronen", out var magronenId) ||
                !ingredients.TryGetValue("Äpfel", out var äpfelId) ||
                !ingredients.TryGetValue("Speckwürfel", out var speckId) ||
                !ingredients.TryGetValue("Frühlingszwiebeln", out var frühlingszwiebelnId) ||
                !ingredients.TryGetValue("Rahm", out var rahmId) ||
                !ingredients.TryGetValue("Gemüsebouillon", out var bouillonId) ||
                !ingredients.TryGetValue("Bergkäse", out var käseId))
            {
                return;
            }

            var kuchenId = await env.Recipes.NextId();
            await env.Recipes.Save(Recipe.Create(
                kuchenId,
                new RecipeName("Einfacher Rührkuchen"),
                new RecipeDescription("Ein klassischer Rührkuchen – saftig, locker und immer beliebt."),
                [
                    new PreparationStep(1, "Butter und Zucker cremig rühren."),
                new PreparationStep(2, "Eier einzeln unterrühren."),
                new PreparationStep(3, "Mehl und Milch abwechselnd unterheben."),
                new PreparationStep(4, "Teig in eine gefettete Form füllen und bei 175 °C ca. 40 Minuten backen."),
                ],
                admin.Id,
                Difficulty.Medium,
                [new RecipeTag("Backen"), new RecipeTag("Kuchen")],
                8,
                [
                    new RecipeIngredient(mehlId, 250m, Gram),
                new RecipeIngredient(zuckerId, 200m, Gram),
                new RecipeIngredient(butterId, 125m, Gram),
                new RecipeIngredient(eierId, 3m, Piece),
                new RecipeIngredient(milchId, 100m, Milliliter),
                ],
                [],
                [new RecipeHint("Den Kuchen mit einem Holzstäbchen auf Gare prüfen – bleibt kein Teig kleben, ist er fertig.")],
                preparationTime: 20,
                cookingTime: 40));

            var pfannkuchenId = await env.Recipes.NextId();
            await env.Recipes.Save(Recipe.Create(
                pfannkuchenId,
                new RecipeName("Pfannkuchen"),
                new RecipeDescription("Dünne, goldbraune Pfannkuchen – schnell gemacht und vielseitig belegbar."),
                [
                    new PreparationStep(1, "Mehl, Milch und Eier zu einem glatten Teig verrühren."),
                new PreparationStep(2, "Teig 15 Minuten ruhen lassen."),
                new PreparationStep(3, "Butter in einer Pfanne erhitzen und Pfannkuchen portionsweise ausbacken."),
                ],
                admin.Id,
                Difficulty.Easy,
                [new RecipeTag("Frühstück"), new RecipeTag("Pfanne")],
                4,
                [
                    new RecipeIngredient(mehlId, 200m, Gram),
                new RecipeIngredient(milchId, 400m, Milliliter),
                new RecipeIngredient(eierId, 2m, Piece),
                new RecipeIngredient(butterId, 20m, Gram),
                ],
                [],
                [new RecipeHint("Den Teig nicht zu lange rühren – ein paar kleine Klümpchen sind in Ordnung.")],
                preparationTime: 10,
                cookingTime: 15));

            await env.Recipes.Save(Recipe.Create(
                await env.Recipes.NextId(),
                new RecipeName("Kartoffelgulasch"),
                new RecipeDescription("Ein herzhafter Eintopf mit Kartoffeln, Paprika und Gewürzgurken – ein Klassiker der deutschen Hausmannskost."),
                [
                    new PreparationStep(1, "Zwiebel fein würfeln und im Öl in einem großen Topf bei mittlerer Hitze glasig andünsten."),
                new PreparationStep(2, "Kartoffeln schälen und in Würfel schneiden. Mit Paprikapulver, Kümmel und gepresstem Knoblauch in den Topf geben und kurz mitdünsten."),
                new PreparationStep(3, "Mit Wasser bedecken und zum Kochen bringen."),
                new PreparationStep(4, "Karotten in Scheiben und Gewürzgurken in Stücke schneiden. Zusammen mit dem Gurkenwasser und Salz in den Topf geben."),
                new PreparationStep(5, "Bei niedriger bis mittlerer Hitze 60–90 Minuten köcheln lassen, bis die Kartoffeln weich sind."),
                ],
                admin.Id,
                Difficulty.Easy,
                [new RecipeTag("Eintopf"), new RecipeTag("Vegetarisch")],
                4,
                [
                    new RecipeIngredient(zwiebelId, 1m, Piece),
                new RecipeIngredient(ölId, 1m, Tablespoon),
                new RecipeIngredient(paprikaId, 2m, Tablespoon),
                new RecipeIngredient(kartoffelnId, 1000m, Gram),
                new RecipeIngredient(knoblauchId, 3m, Piece),
                new RecipeIngredient(kümmelId, 1m, Tablespoon),
                new RecipeIngredient(karottenId, 2m, Piece),
                new RecipeIngredient(gewürzgurkenId, 3m, Piece),
                new RecipeIngredient(salzId, 1m, Teaspoon),
                new RecipeIngredient(gurkenwasserId, 1m, Tablespoon),
                ],
                [],
                [new RecipeHint("Am zweiten Tag schmeckt das Gericht noch besser – einfach aufwärmen und genießen.")],
                preparationTime: 20,
                cookingTime: 75));

            await env.Recipes.Save(Recipe.Create(
                await env.Recipes.NextId(),
                new RecipeName("Älpler One-Pot"),
                new RecipeDescription("Ein echtes Schweizer Nationalgericht in einer kreativen und schnell gemachten Variante – alles in einem Topf."),
                [
                    new PreparationStep(1, "Magronen, Kartoffeln, Äpfel, Speck, Frühlingszwiebeln und Bouillon in einen großen Topf geben, mischen und zum Kochen bringen. Bei mittlerer Hitze ohne Deckel ca. 20 Minuten köcheln lassen, dabei gelegentlich umrühren."),
                new PreparationStep(2, "Rahm und Bergkäse unterrühren, abschmecken und nach Belieben mit Röstzwiebeln garnieren."),
                ],
                admin.Id,
                Difficulty.Easy,
                [new RecipeTag("Schweizer Küche"), new RecipeTag("One-Pot")],
                4,
                [
                    new RecipeIngredient(magronenId, 400m, Gram),
                new RecipeIngredient(kartoffelnId, 240m, Gram),
                new RecipeIngredient(äpfelId, 2m, Piece),
                new RecipeIngredient(speckId, 160m, Gram),
                new RecipeIngredient(frühlingszwiebelnId, 1m, Piece),
                new RecipeIngredient(rahmId, 3m, Deciliter),
                new RecipeIngredient(bouillonId, 8m, Deciliter),
                new RecipeIngredient(käseId, 79m, Gram),
                ],
                [],
                [new RecipeHint("Das Gericht nicht zu lange kochen – die Magronen sollen noch bissfest sein.")],
                preparationTime: 10,
                cookingTime: 20));
        }
    }
}
