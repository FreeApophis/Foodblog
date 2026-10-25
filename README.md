# FunctionalBlog

A German-language food blog and recipe site that doubles as a demonstration of **functional
programming in C# / .NET 10**. The HTTP layer is built from scratch on a small, curried,
reader-style pipeline of delegates — no MVC, no controllers, no DI container in the request path —
and the domain code stays inside `Option<T>` and applicative validation rather than reaching for
nulls and exceptions.

# foodblog.ch

You can checkout a running instance at [https://foodblog.ch](https://foodblog.ch).

## What it does

- **Blog articles** and **static pages** with cover images and inline BBCode (including gallery images).
- **Recipes** with ingredients, units, preparation steps, hints, nutrition, and **printable PDFs**
  (QuestPDF + SkiaSharp).
- **Ingredients** catalogue and a **units** catalogue, both editable from the admin area.
- A shared **image library** (stored as SQLite BLOBs) with orphan **cleanup**.
- **Tags** and SEO-friendly **slug** URLs across every content type, with **JSON-LD + Open Graph**
  metadata.
- **Full-text search** (LeanCorpus) plus an instant typeahead, and an admin page to inspect/rebuild
  the index.
- **Accounts**: registration, login, sessions, password reset, avatars, and a **role/permission**
  model guarding every write route.
- **Internationalisation** (DB-driven translations, four languages) and a **light/dark theme**.
- An **admin dashboard** with stats and per-section cards, gated by permissions.

The UI is in **German**.

## Tech stack

- **.NET 10** (SDK pinned in `global.json`), C# with `ImplicitUsings` and nullable enabled.
- **ASP.NET Core** only as a thin host — `HttpAdapter` converts `HttpContext` to/from the domain
  `Request`/`Response`, and a single hand-rolled pipeline does the rest.
- **SQLite** via **Dapper**, with schema migrations run by **dbup-sqlite** (embedded `*.sql`).
- **[Funcky](https://github.com/polyadic/funcky)** for `Option<T>` and friends; `Funcky.DiscriminatedUnion`
  for the `Validated` applicative.
- **LeanCorpus** (full-text search), **QuestPDF** + **SkiaSharp** (PDF), **htmx** (progressive UI).
- Central package management (`Directory.Packages.props`) and shared build settings
  (`Directory.Build.props`).

## Getting started

Requires the .NET 10 SDK.

```bash
dotnet build          # build
dotnet run --project FunctionalBlog
```

The app listens on **https://localhost:65243** (HTTP: 65244). On first run it creates a SQLite
database, applies migrations, seeds reference data, builds the search index, and backfills slugs.

**First login.** There is no default password. If the database contains no users at all, startup
creates one admin account and writes its random password **once** to the console log, in a line
starting with `WARNING: No users found` (look for `bootstrap-admin-password:`). Log in with it and
change it right away. The account's email is `admin@blog.de` unless you set `BOOTSTRAP_ADMIN_EMAIL`.
If any user already exists, no account is created. Note that the bundled `0002_seed` migration
imports the foodblog.ch users, so a database built from it already has users.

Earlier releases created `admin@blog.de` with the public password `Admin1234`. On startup, if that
account still has that password, it is locked: its password is replaced with an unknown random one and
its sessions are ended. The account and its content are kept, and a warning is logged.

**Email.** Set the SMTP server and the **base URL** under *Administration → Einstellungen*. Password
reset and verification mails link to the base URL, which must be an absolute `https://` address (it is
never taken from the request's Host header). Until it is set, those mails are not sent, an error is
logged, and the admin dashboard shows a warning. With "SSL" enabled, TLS is required: implicit TLS on
port 465, mandatory STARTTLS on any other port.

**HTTPS only.** Every cookie is `Secure` and HTML responses carry a Content-Security-Policy, so serve
the site over HTTPS (directly or behind a reverse proxy). Browsers accept `Secure` cookies on
`http://localhost`, so local development on the HTTP port still works.

Data lives under `./data` by default (database, full-text index, config). Override the location with
the `DATA_DIR` environment variable.

```bash
dotnet test           # run every xunit test project in the solution
```

## Architecture in one screen

The core abstraction is a curried, reader-style pipeline expressed as delegates (in
`FunctionalBlog.Pipeline`):

```
Effect<T>  = Func<Env, ValueTask<T>>           // a computation needing the environment
App        = Func<Request, Effect<Response>>   // a handler: request first, then environment
Middleware = Func<App, App>                    // wraps a handler
```

A handler runs as `app(request)(env)`. `Functional.Compose(notFound, middlewares…)` folds the
middlewares right-to-left over a terminator. The live pipeline is:

```
Language → Theme → Recover → RequestLogging → Auth → Csrf → Slug → Router
```

`Router` dispatches against the `RouteTable` declared in `Routes.cs`, and `Env` is the single reader
environment threaded through everything (all repositories, the clock, the logger, plus per-request
state like the current user, language, theme, and CSRF token).

### Projects

The solution is split into nine projects with a strict core dependency direction
(**Domain ← Application ← DataAccess.\* ← Web ← Test**); `Pipeline` and `Bbcode` are leaf libraries
the Web project also references.

| Project | Role |
| --- | --- |
| `FunctionalBlog.Domain` | Entities and value objects. No project references — pure domain. |
| `FunctionalBlog.Application` | Repository/service contracts, one folder per area. |
| `FunctionalBlog.Pipeline` | The generic functional core (`Effect`, `App`, `Middleware`, `Functional`). |
| `FunctionalBlog.DataAccess.InMemory` | In-memory repositories used by tests. |
| `FunctionalBlog.DataAccess.Sqlite` | Production SQLite repositories, migrations, password hashing. |
| `Bbcode` | Standalone, dependency-free BBCode → HTML renderer. |
| `FunctionalBlog` (Web) | Handlers, views, forms, pipeline wiring, ASP.NET bootstrap. |
| `FunctionalBlog.Test` / `Bbcode.Test` | xunit test suites. |

## Functional conventions

Two patterns run through the codebase and are worth knowing before contributing:

- **Stay in `Option<T>`.** Repository "find" methods return `Option<T>`, never `T?`. Guard with the
  list pattern (`if (option is [var x])`) instead of escaping to a nullable.
- **Forms accumulate errors.** Form decoders return `Validated<IReadOnlyList<string>, TValid>`, an
  applicative functor that collects *all* field errors rather than failing on the first.

## Development

The project is built **test-first (TDD)**: write a failing test, watch it go red, then write the
minimum production code to make it green. New `IArticleRepository` implementations derive their tests
from `ArticleRepositoryContract` so every storage backend meets the same spec.

See **[CLAUDE.md](CLAUDE.md)** for the full contributor guide — the functional conventions in depth,
the form-validation pattern, the per-project file layout, and the coding rules enforced by the
analyzers.
