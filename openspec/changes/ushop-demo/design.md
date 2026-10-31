## Context

See `proposal.md` for motivation. Toolchain observed: .NET 10.0.202, Aspire CLI 13.6.4, Docker 29 on arm64, Node 24. `mcr.microsoft.com/mssql/server:2022-latest` is pulled locally.

An earlier apply pass (when the subject was a blog) produced uncommitted, partly verified code under `Blog.*` names that this design carries forward:
- AppHost, port launcher and worktree script work; `dev.mjs` started an isolated stack on derived ports; port unit tests pass.
- Unattended install and code seeding work (content created, published, templates assigned).
- Findings: views must be compiled at build (Umbraco 18 has no runtime Razor compiler without `Umbraco.Cms.DevelopmentMode.Backoffice`); content created via `IContentService` needs `TemplateId` set explicitly; template view files are named by alias (`Views/<alias>.cshtml`, lower camel case) and Umbraco rewrites them (adds a BOM) when templates are created; Razor generics need `@(...)` wrapping.
- Background shells ignore SIGINT; stop a backgrounded AppHost with SIGTERM to its own PID, which cleans up its SQL container.

Aspire randomises container name suffixes and SQL host ports and proxies project endpoints; only the dashboard, OTLP and resource service URLs were hard-coded (in `launchSettings.json`, now removed).

## Goals / Non-Goals

**Goals:**
- `node scripts/dev.mjs` in any worktree yields a working, isolated, seeded, migrated shop.
- Layout work touches only views/CSS, so branches diff cleanly.

**Non-Goals:**
- Real commerce: payments, stock, tax, shipping, members/login, emails, admin order UI.
- Production hardening, persistence across runs, tests of Umbraco itself.
- Fast cold start; mitigated by pre-pulling images and pre-building.

## Decisions

**Naming.** Solution `uShopDemo.slnx`, projects `uShopDemo.AppHost` / `uShopDemo.Web`, namespaces `uShopDemo.*`, env vars `USHOP_WEB_PORT` / `USHOP_BRANCH`. Aspire generates `Projects.uShopDemo_Web`.

**Port isolation via env, not launchSettings.** The launcher computes ports and sets `ASPNETCORE_URLS` (dashboard), `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`, `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`, `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`, `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`, `USHOP_WEB_PORT`, then runs `dotnet run --project src/uShopDemo.AppHost --no-launch-profile`. Alternatives: per-worktree launchSettings edits (dirty git state), fully dynamic ports (unstable URLs on stage).

**Port block derivation.** SHA-1 of the real, lower-cased worktree path → slot 0..99; block = 15000 + slot*10 (web +0, dashboard +1, OTLP +2, resource service +3). Probe by binding; on conflict move to the next slot. No state file.

**Database isolation by ephemerality.** SQL Server container with no volume and default (session) lifetime; `umbracoDbDSN` database created by Aspire. Every run starts empty, which exercises install, migrations and seeding each time. Alternative: named volume per worktree — rejected, stale data hides changes and complicates cleanup.

**Umbraco wiring.** Connection string from `WithReference(db)`; provider name via `ConnectionStrings__umbracoDbDSN_ProviderName`. Unattended install, `ModelsMode: Nothing` and `ApplicationUrlDetection: FirstRequest` in `appsettings.json`. Views compiled at build; view edits require restarting the launcher. Views read properties with `Value<T>()`, no generated models.

**Content model and seeding.** Document types: `shop` (root; site title, tagline), `category` (title, intro), `product` (title, SKU, price as decimal, summary, description, image path), `cart`, `checkout`, `orderConfirmation` (singleton pages under root, no properties beyond title). A composer-registered `UmbracoApplicationStartedNotification` handler seeds if no `shop` root exists: templates created from on-disk view content, `TemplateId` set on every node, publish all. Images are static SVG/PNG files in `wwwroot/img` referenced by path, not Umbraco media (no media upload code, layout branches can restyle freely).

**Cart storage: EF Core in the Umbraco database.** `ShopDbContext` registered with Umbraco's EF Core integration (`AddUmbracoDbContext<ShopDbContext>(... UseUmbracoDatabaseProvider ...)`), sharing the Umbraco connection. Entities:
- `Cart`: `Id` (Guid, also the cookie value), `CreatedUtc`, `Status` (Open/Ordered), `OrderNumber`, `CustomerName`, `Email`, `Address`, `OrderedUtc`, `Total`.
- `CartLine`: `Id`, `CartId`, `ProductKey` (Umbraco content Guid), `ProductName`, `Sku`, `UnitPrice`, `Quantity`.
Tables prefixed `ushop` to sit clearly beside Umbraco's. Unit price and name captured at add time. Fake card number is validated for presence only and never stored.
Alternatives: session cart (not persisted, user asked for DB), orders as Umbraco content (mixes transactional data with content, slower), raw NPoco/Umbraco migrations (EF Core requested).

**Migrations.** Code-first, `Migrations/` in `uShopDemo.Web`, generated with `dotnet ef` from a local tool manifest. Applied via `Database.MigrateAsync()` in a startup notification handler that runs after Umbraco install and before seeding. EF design-time uses an `IDesignTimeDbContextFactory` with a dummy SQL Server connection string so `dotnet ef migrations add` works without a running stack.

**Cart and checkout endpoints.** A `SurfaceController` (`CartSurfaceController`) handles POSTs: add, update quantity, remove, checkout. A small `CartService` encapsulates cookie handling and EF access; views get the current cart via an injected service (`@inject ICartService`). Cart/checkout/confirmation are Umbraco content pages so they get URLs and templates like every other page; confirmation reads the order number from TempData or query. Anti-forgery via Umbraco's form helpers.

**Layouts live in `Views/` and `wwwroot/`.** `Shared/_Layout.cshtml`, `shop.cshtml`, `category.cshtml`, `product.cshtml`, `cart.cshtml`, `checkout.cshtml`, `orderConfirmation.cshtml`, partials under `Views/Partials/`, `wwwroot/css/site.css`. Branch badge reads `USHOP_BRANCH`. Demo notice lives in `_Layout.cshtml` and must remain in every layout.

**Layout branches.** `layout/minimal` (clean single column, text-first), `layout/catalog` (dense grid, category sidebar, filters look), `layout/showcase` (large imagery, editorial hero). Branched from `main` after the user commits the baseline. Commits are made by the user.

**Scripts.** Plain ESM Node, no dependencies. `scripts/lib/ports.mjs`, `scripts/lib/git.mjs` (branch via `symbolic-ref`, works before first commit), `dev.mjs`, `worktrees.mjs` (`up|list|down [branch...]`). Worktrees created as siblings `../<repo>.<branch-slug>`.

## Risks / Trade-offs

- [Cold start slow (SQL boot + install + migrate + seed ~1-2 min), three at once heavier] → Pre-start before the talk; docs checklist.
- [Umbraco EF Core integration API differs in v18] → Task 3.3 verifies registration and migrations first; fall back to plain `AddDbContext` with the Umbraco connection string if needed.
- [Docker memory with three SQL Servers (~2 GB each)] → Document Docker memory ≥ 8 GB.
- [A migration added in one worktree only affects that worktree's DB] → Intended; call it out in the talk as a benefit.
- [Port probe race] → Acceptable; relaunch is stable.
- [Dashboard unsecured mode] → Localhost demo only, documented.
- [Templates rewritten by Umbraco on creation (BOM)] → Content identical; `.gitattributes`/editor ignore BOM; verify `git status` clean after a run.
