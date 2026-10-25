# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build                  # Debug build
dotnet build -c Release       # Release build
dotnet run                    # Run (HTTPS: localhost:65243, HTTP: localhost:65244)
dotnet clean                  # Clean build artifacts
dotnet test                   # Run every xunit test project in the solution
```

Tests live in `Foodblog.Test/` and `Bbcode.Test/` (both xunit); `dotnet test` runs every test project. Code style is enforced by the `Polyadic.CodeStyle` analyzer and the Funcky analyzers (wired through `Directory.Build.props`); there is no separate linter. NuGet package versions are managed centrally in `Directory.Packages.props` (CPM is enabled). Every project targets `net10.0`, set once in `Directory.Build.props`.

## Workflow

This project follows **Test-Driven Development**. For every change to production code:

1. Write a failing test in `Foodblog.Test/` that captures the desired behavior.
2. Run `dotnet test` and confirm it fails for the expected reason.
3. Write the minimum production code in the project that owns the change (`Foodblog.Domain`, `Foodblog.Application`, `Foodblog.Pipeline`, `Foodblog.DataAccess.InMemory`, `Foodblog.DataAccess.Sqlite`, `Bbcode`, or `Foodblog`) to make it pass.
4. Run `dotnet test` again and confirm all tests pass.
5. Refactor while keeping tests green.

Do not add production code that is not driven by a failing test. Do not skip step 2 — a test that has never been red proves nothing.

When adding a new `IArticleRepository` implementation, derive its tests from `ArticleRepositoryContract` (in `Foodblog.Test/`) so the contract is enforced uniformly across impls.

## Architecture

The solution is split into nine projects. The core dependency direction is strict (Domain ← Application ← DataAccess.* ← Web ← Test); `Pipeline` and `Bbcode` are leaf libraries the Web project also references:

- **`Foodblog.Domain`** — entities and value objects (Articles, Identity, Roles/permissions, Recipes, Translations). No project references. Pure domain.
- **`Foodblog.Application`** — repository/service contracts (one folder per domain area). References Domain.
- **`Foodblog.Pipeline`** — the functional HTTP core as **generic** delegates: `Effect<TEnv, T>`, `App<TEnv, TRequest, TResponse>`, `Middleware<TEnv, TRequest, TResponse>`, and `Functional.Compose`. No project references — it knows nothing about the Web types. The Web project binds the concrete aliases via `global using` (e.g. `App = Pipeline.App<Env, Request, Response>`, `Middleware = Pipeline.Middleware<Env, Request, Response>`). Referenced by Web.
- **`Foodblog.DataAccess.InMemory`** — in-memory repository implementations for use in tests, one folder per domain area. References Application.
- **`Foodblog.DataAccess.Sqlite`** — SQLite repository implementations for production, plus `DatabaseMigrator`, `DapperTypeHandlers`, `Pbkdf2PasswordHasher`, and `TranslationSeeder`. Migrations are embedded `Migrations/NNNN_*.sql` resources run by dbup-sqlite. References Application. Uses Dapper and dbup-sqlite. Repositories take a `SqliteConnectionFactory` and open a pooled connection per operation (`await using var connection = await _connections.OpenAsync();`); never keep a `SqliteConnection` in a field, because requests run in parallel and a connection is not thread-safe.
- **`Bbcode`** — standalone, dependency-free `BbcodeRenderer` (encode-first BBCode → HTML, including inline gallery images). Referenced by Web; has its own `Bbcode.Test` project.
- **`Foodblog`** (Web) — handlers, views, forms, the concrete pipeline wiring, and the ASP.NET bootstrap. References Application, DataAccess.Sqlite, Pipeline, and Bbcode. Pulls in LeanCorpus (full-text search), QuestPDF + SkiaSharp (recipe PDF printing), and Funcky.DiscriminatedUnion.
- **`Foodblog.Test`** — xunit. References Web (gets DataAccess.Sqlite + Pipeline transitively) and DataAccess.InMemory directly.
- **`Bbcode.Test`** — xunit. References Bbcode only.

The project demonstrates functional programming principles in .NET 10. The core abstraction is a curried, reader-style pipeline expressed with delegates (`Foodblog.Pipeline`):

```
Effect<T>  = Func<Env, ValueTask<T>>           // Effect.cs
App        = Func<Request, Effect<Response>>   // App.cs
Middleware = Func<App, App>                    // Middleware.cs
```

A handler is invoked as `app(request)(env)` — request first, then environment. `Functional.Compose(notFound, params Middleware[])` folds middlewares right-to-left over a terminator (`Program.cs` passes a `NotFound` terminator). In `Program.cs` the live pipeline is `Language → Theme → Recover → RequestLogging → Auth → Csrf → Slug → Router`, and the `Router` middleware dispatches against the `RouteTable` built in `Routes.Build()`.

The single `Env` instance is built once in `Program.cs` (`BuildEnv`), threaded through every handler, and refreshed with `with` expressions as startup wires in the translation cache and search index. Per-request state (current user, language, theme, CSRF token) is layered on by the upstream middlewares, again via `with`.

### Key types

- **`Env`** (Web) — the reader environment record. Carries every repository/service (`Articles`, `Users`, `Roles`, `Sessions`, `PasswordResets`, `Recipes`, `Ingredients`, `Units`, `Images`, `Pages`, `Tags`, `Slugs`, `Translations`, `Search`, `QuickSearch`), the infra (`Clock`, `Log`, `PasswordHasher`), and per-request state (`CurrentUser`, `Language`, `Theme`, `CsrfToken`). Exposes derived helpers: `T` (translate via `TranslationCache`), `EnsureSlug`/`SlugMaker`, and `Ctx` (a `ViewContext` snapshot passed to views).
- **`Request`** / **`Response`** (Web) — HTTP abstractions. `Response` factories: `Html`, `Text`, `Css`, `Js`, `Redirect`, `Forbidden`, `NotFound`, `Bytes` (images/favicon/PDF, with optional headers), and `JsonDownload`. `Request` exposes `Form`, query, route captures, cookies, and uploaded files (`UploadedFile`).
- **`RouteTable`** / **`Routes`** / **`Router`** (Web) — `Routes.Build()` is the route registry (the composition root for routing) returning an immutable `RouteTable` of `(HttpMethod, pattern, factory)` entries with `{param}` capture. `Router.Create(table)` is the `Middleware` that matches a request and dispatches; falls through to `Response.NotFound`.
- **`Auth`** (Web) — route guards: `Auth.RequireAuth(app)` and `Auth.RequirePermission<TAction>(resource, app)` wrap handlers and short-circuit unauthenticated/unauthorized requests. Permissions are evaluated against the role model in Domain (`IPrincipal`, `IResource`, `IAction`, `PermissionRule`, `Role`, `Guest`).
- **Middlewares** (Web) — `Middlewares.cs` holds `Recover` (catches exceptions, logs, 500 page) and `RequestLogging`. Cross-cutting concerns live in their own files: `AuthMiddleware`, `CsrfMiddleware`, `SlugMiddleware`, `ThemeMiddleware`, `LanguageMiddleware` — each layers state onto `Env` for downstream handlers.
- **Handlers** (Web) — `App`-valued handler classes per feature area: `BlogHandlers`, `RecipeHandlers`, `IngredientHandlers`, `PageHandlers`, `ImageHandlers`, `TagHandlers`, `AuthHandlers`, `UsersHandlers`, `UserSettingsHandlers`, `AdminHandlers`/`AdminDashboardHandlers`, `Admin*`/`AdminUnitHandlers`/`AdminIngredientHandlers`/`AdminSearchHandlers`, `SearchHandlers`, `TranslationHandlers`, `ThemeHandlers`, `RecipePdfHandlers`, `ImageCleanupHandlers`, `StaticHandlers`.
- **`StaticHandlers`** (Web) — serves CSS, JS, fonts, favicon, and images from embedded `wwwroot` resources, cached in `Lazy<>`.
- **Domain model** — Articles (`Article`, `ArticleId`, `ArticleTitle`, `ArticleTeaser`, `ArticleText`), plus Identity, Roles, Recipes (incl. `RecipeIngredient`, `PreparationStep`, `Unit`), Translations, and value objects under `Foodblog.Domain/<Area>/`.
- **Repository contracts** (Application) — `IArticleRepository`, `IUserRepository`, `IRoleRepository`, `ISessionStore`, `IPasswordResetTokenStore`, `IRecipeRepository`, `IIngredientRepository`, `IUnitRepository`, `IImageRepository`, `IPageRepository`, `ITagRepository`, `ISlugRepository`, `ITranslationRepository`, `ISearchIndex`, `IQuickSearch`, plus `IPasswordHasher`/`IEmailSender`. Each has both an `InMemory*` (tests) and `Sqlite*` (production) implementation. The `IArticleRepository` behavioral spec lives in `Foodblog.Test/ArticleRepositoryContract.cs`.
- **`*Form`** (Web) — one per form (`ArticleForm`, `RecipeForm`, `RegisterForm`, `LoginForm`, `ChangePasswordForm`, `PasswordReset*Form`, `IngredientForm`, `PageForm`, `UnitForm`, `ImageUploadForm`, `RoleForm`, `RuleForm`, `AssignRoleForm`). HTTP decoding/validation returning `Validated<IReadOnlyList<string>, Valid>`. Forms stay in Web — they're a presentation/transport concern, not a domain rule.
- **`Validated<TFailure, TSuccess>`** (Web) — applicative-functor discriminated union for form validation. `Failure` accumulates all field errors; `Success` carries the fully-typed valid form.
- **Views** (Web) — `*Views` classes (`BlogViews`, `RecipeViews`, `NavViews`, …), `Layout`, and the `Html`/`HtmlString` helpers. Server-rendered HTML with XSS protection via `WebUtility.HtmlEncode`; always build markup through the `Html` helpers rather than raw strings (encode leaf text, pass raw body for container elements). `BbcodeRenderer` (the `Bbcode` project) renders user BBCode bodies. CSS, JS, fonts, and images live under `wwwroot/` as embedded resources.
- **Search** (Web) — `LeanCorpusSearchIndex` (`ISearchIndex`) is a LeanCorpus full-text index rebuilt on startup and updated on writes; `SqliteQuickSearch` (`IQuickSearch`) backs the instant `/search/quick` typeahead.
- **PDF** (Web) — `RecipePdf*` render printable recipe PDFs with QuestPDF + SkiaSharp.
- **`HttpAdapter`** (Web) — adapts ASP.NET Core's `HttpContext` to/from the domain `Request`/`Response`.
- **`SystemClock`** / **`ConsoleLog`** (Web) — concrete `IClock` / `ILog`, wired in `Program.cs`. `Seeder` and `SlugBackfill` run idempotent startup seeding/backfill. `Seeder` never creates a user with a known password: on a database without any users it bootstraps one admin (`BOOTSTRAP_ADMIN_EMAIL`, default `admin@blog.de`) with a random password logged once, and it locks `admin@blog.de` if that account still has the leaked old default password.

## Functional style with Funcky `Option<T>`

All "find" methods on repositories return `Option<T>`, not `T?`. **Never escape the monadic space** by extracting to a nullable. Stay inside `Option<T>` using the tools below.

### Creating Option values

```csharp
Option.Some(value)          // wrap a known value
Option<T>.None              // empty
dict.GetValueOrNone("key")  // Funcky extension — replaces TryGetValue
sequence.FirstOrNone(pred)  // Funcky LINQ extension — replaces FirstOrDefault
```

### List pattern matching — the idiomatic `if` for Option

`Option<T>` implements the list pattern, so use C# pattern matching instead of `TryGetValue` or `.Match(none: () => default!, ...)`:

```csharp
// Guard: act only when Some, skip when None
if (option is [var value])
{
    // use value
}

// Early return when None
if (option is not [var value])
{
    return Response.NotFound();
}
// use value

// Both branches
if (option is [var value])
    DoSomething(value);
else
    HandleMissing();
```

### Staying in monadic space

```csharp
option.Select(x => x.Name)            // map: Option<T> → Option<U>
option.SelectMany(x => Find(x.Id))    // flatMap / bind: Option<T> → Option<U>
option.GetOrElse("fallback")          // extract with default (only at edges)
option.Match(none: ..., some: ...)    // bifurcate (prefer list pattern for guards)
option.ToEnumerable()                 // Option<T> → IEnumerable<T> (0 or 1 items)
```

### Async pattern

When the `some` branch needs an async call, use `Match` with `Task`-returning lambdas:

```csharp
var userOption = await emailOption.Match(
    none: () => Task.FromResult(Option<User>.None),
    some: async email => await env.Users.FindByEmail(email));
```

### Pipeline example from the codebase

```csharp
// AuthHandlers.Register — stays entirely in Option space
if (decoded.Email is [var regEmail])
{
    if ((await env.Users.FindByEmail(regEmail)) is not [var existingUser])
    {
        // user not found — create one
        var roleNames = (await env.Roles.FindByName("Benutzer"))
            .Select(role => role.Name)
            .ToEnumerable()
            .ToImmutableList();
        existingUser = User.Create(...);
        await env.Users.Save(existingUser);
    }
    // existingUser is guaranteed non-null here
    ...
}
```

### What NOT to do

```csharp
// BAD — escapes monadic space, brings nullability back in
var x = option.Match(none: () => default(T), some: v => v);
if (x is null) return Response.NotFound();

// BAD — TryGetValue is banned by the Funcky analyzer (λ0001)
if (!option.TryGetValue(out var x)) return Response.NotFound();

// GOOD — list pattern keeps everything in Option
if (option is not [var x]) return Response.NotFound();
// use x here
```

## Form validation with `Validated<TFailure, TSuccess>`

Form decoders return `Validated<IReadOnlyList<string>, TValid>` — an applicative functor that accumulates **all** field errors independently rather than short-circuiting on the first. The failure type carries a list of translation-key error strings; the success type is a strongly-typed record with domain value objects.

### Defining a form decoder

Each field is a pure function returning `Validated<IReadOnlyList<string>, TField>`. Compose them with `.Apply` following the blog-post pattern (https://blog.ploeh.dk/2023/10/30/a-c-port-of-validation-with-partial-round-trip/):

```csharp
public static class ArticleForm
{
    public sealed record Valid(ArticleTitle Title, ArticleTeaser Teaser, ArticleText Text);

    public static Validated<IReadOnlyList<string>, Valid> Decode(Request request)
    {
        var title = request.Form.GetValueOrNone("title").GetOrElse(string.Empty).Trim();
        var teaser = request.Form.GetValueOrNone("teaser").GetOrElse(string.Empty).Trim();
        var text  = request.Form.GetValueOrNone("text").GetOrElse(string.Empty).Trim();

        Func<ArticleTitle, ArticleTeaser, ArticleText, Valid> create =
            (t, te, tx) => new Valid(t, te, tx);

        return create
            .Apply(TryParseTitle(title), Combine)
            .Apply(TryParseTeaser(teaser), Combine)
            .Apply(TryParseText(text), Combine);
    }

    private static Validated<IReadOnlyList<string>, ArticleTitle> TryParseTitle(string raw) =>
        raw.Length >= 3
            ? Validated.Succeed<IReadOnlyList<string>, ArticleTitle>(new ArticleTitle(raw))
            : Validated.Fail<IReadOnlyList<string>, ArticleTitle>(["article.error.title_too_short"]);

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> a, IReadOnlyList<string> b) => [..a, ..b];
}
```

### Compound field validators (multiple checks on one field)

Use a proof-witness `Func<bool, bool, TField>` that always returns the field value — the booleans are just accumulation vessels:

```csharp
private static Validated<IReadOnlyList<string>, string> TryParsePassword(string password, string confirmation)
{
    Func<bool, bool, string> alwaysPassword = (_, _) => password;

    return alwaysPassword
        .Apply(CheckPasswordLength(password), Combine)
        .Apply(CheckPasswordMatch(password, confirmation), Combine);
}
```

### Lifting Option validators into Validated

Use the list pattern to convert `Option<T>` to `Validated`:

```csharp
private static Validated<IReadOnlyList<string>, Email> TryParseEmail(string raw) =>
    Email.ParseOrNone(raw) switch
    {
        [var email] => Validated.Succeed<IReadOnlyList<string>, Email>(email),
        []          => Validated.Fail<IReadOnlyList<string>, Email>(["auth.error.invalid_email"]),
    };
```

### Consuming in handlers

Use `Match` with `Task`-returning lambdas. Raw form values for re-rendering come from `request.Form`; typed domain values come from `s.Value`:

```csharp
public static App CreateArticle => request => async env =>
    await ArticleForm.Decode(request).Match(
        failure: f => Task.FromResult(Response.Html(
            BlogViews.Form(
                f.Error,
                request.Form.GetValueOrNone("title").GetOrElse(string.Empty),
                ...),
            400)),
        success: async s =>
        {
            var article = Article.Create(title: s.Value.Title, ...);
            await env.Articles.Save(article);
            return Response.Redirect($"/articles/{article.Id.Value}");
        });
```

### Testing

Use `ValidatedAssert.IsSuccess` / `ValidatedAssert.IsFailure` (in `Foodblog.Test/ValidatedAssert.cs`):

```csharp
var form = ValidatedAssert.IsSuccess(ArticleForm.Decode(validRequest));
Assert.Equal(new ArticleTitle("Guter Titel"), form.Title);

var errors = ValidatedAssert.IsFailure(ArticleForm.Decode(invalidRequest));
Assert.Contains("article.error.title_too_short", errors);
```

### UI language

All user-facing text (form validation messages, article content, page labels) is in **German**.

### File layout

One public type per file. Files are grouped into per-area folders, and each folder is a namespace (e.g. `Foodblog.Domain.Articles`, `Foodblog.Application.Identity`, `Foodblog.Recipes`). The Web project's `GlobalUsings.cs` `global using`s every namespace (and binds the `App`/`Middleware` aliases), so within Web most code reads as if there were no namespaces. New files should sit in the folder for their area and declare the matching namespace.

**`Foodblog.Domain/`** — value objects/entities under `Articles/`, `Identity/`, `Roles/`, `Recipes/`, `Translations/` (and the polymorphic Tags/Slugs/Images/Pages types added by later features).

**`Foodblog.Application/`** — repository/service contracts under one folder per area (`Articles/`, `Identity/`, `Images/`, `Pages/`, `Recipes/`, `Roles/`, `Search/`, `Slugs/`, `Tags/`, `Translations/`).

**`Foodblog.Pipeline/`**: `Effect.cs`, `App.cs`, `Middleware.cs`, `Functional.cs` (generic delegates only).

**`Bbcode/`**: `BbcodeRenderer.cs` (+ `Bbcode.Test/BbcodeRendererTests.cs`).

**`Foodblog.DataAccess.InMemory/`**: `InMemory*` repository implementations, one folder per domain area.

**`Foodblog.DataAccess.Sqlite/`**: `Sqlite*` repository implementations, `DatabaseMigrator`, `DapperTypeHandlers`, `SqliteConnectionFactory`, `Pbkdf2PasswordHasher` (`Identity/`), `TranslationSeeder` (`Translations/`), and `Migrations/NNNN_*.sql` (embedded resources, run in order by dbup).

**`Foodblog/`** (Web), grouped by feature folder, with shared infrastructure at the root:

1. HTTP model + bootstrap (root): `Request.cs`, `Response.cs`, `Env.cs`, `ViewContext.cs`, `HttpAdapter.cs`, `HttpMethod.cs`, `Program.cs`, `Seeder.cs`
2. Pipeline + routing (root): `Middlewares.cs`, `Router.cs`, `RouteTable.cs`, `Routes.cs`, `SlugMiddleware.cs`, `SlugDispatch.cs`, `SlugIndex.cs`
3. Shared view/form helpers (root): `Layout.cs`, `Html.cs`, `HtmlString.cs`, `Validated.cs`, `Pagination.cs`, `PagedResult.cs`, `Crumb.cs`, `Seo.cs`, `PageMeta.cs`, `AmountFormat.cs`
4. Infrastructure (root): `IClock.cs`, `SystemClock.cs`, `ILog.cs`, `ConsoleLog.cs`
5. Feature folders, each with its handlers/views/forms: `Articles/`, `Recipes/`, `Ingredients/`, `Pages/`, `Images/`, `Tags/`, `Identity/`, `Roles/`, `Units/`, `Admin/`, `Search/`, `Translations/`, `Theme/`
6. `wwwroot/` — `styles.css`, `htmx.min.js` and small JS helpers, fonts, favicon, images (all embedded resources)
