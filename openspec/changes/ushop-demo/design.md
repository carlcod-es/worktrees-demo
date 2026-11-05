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
- Site is HTTPS-only (backoffice login requires it); the ASP.NET dev cert covers `localhost` on every port, so per-worktree ports are unchanged.
- `node scripts/dev.mjs` in any worktree yields a working, isolated, seeded, migrated shop.
- Theme work touches only CSS, so branches diff cleanly and can be made live without a restart.

**Non-Goals:**
- Real commerce: payments, stock, tax, shipping, members/login, emails, admin order UI.
- Production hardening, persistence across runs, tests of Umbraco itself.
- Fast cold start; mitigated by pre-pulling images and pre-building.

## Decisions

ADRs: ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0015, ADR-0017, ADR-0031, ADR-0032, ADR-0033, ADR-0034 (`docs/adrs/`). This section keeps the implementation detail.

**Naming.** Solution `uShopDemo.slnx`, projects `uShopDemo.AppHost` / `uShopDemo.Web`, namespaces `uShopDemo.*`, env vars `USHOP_WEB_PORT` / `USHOP_BRANCH`. Aspire generates `Projects.uShopDemo_Web`.

**Port isolation via env, not launchSettings.** The launcher computes ports and sets `ASPNETCORE_URLS` (dashboard), `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`, `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`, `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` (all URLs HTTPS via the dev cert, which covers `localhost` on any port), `USHOP_WEB_PORT`, then runs `dotnet run --project src/uShopDemo.AppHost --no-launch-profile`. Alternatives: per-worktree launchSettings edits (dirty git state), fully dynamic ports (unstable URLs on stage).

**Port block derivation.** SHA-1 of the real, lower-cased worktree path → slot 0..99; block = 15000 + slot*10 (web +0, dashboard +1, OTLP +2, resource service +3). Probe by binding; on conflict move to the next slot. No state file.

**Database isolation by ephemerality.** SQL Server container with no volume and default (session) lifetime; `umbracoDbDSN` database created by Aspire. Every run starts empty, which exercises install, migrations and seeding each time. Alternative: named volume per worktree — rejected, stale data hides changes and complicates cleanup.

**Umbraco wiring.** Connection string from `WithReference(db)`; provider name via `ConnectionStrings__umbracoDbDSN_ProviderName`. Unattended install, `ModelsMode: Nothing` and `ApplicationUrlDetection: FirstRequest` in `appsettings.json`. Views compiled at build; view edits require restarting the launcher. Views read properties with `Value<T>()`, no generated models.

**Content model and seeding.** Document types: `shop` (root; site title, tagline), `category` (title, intro), `product` (title, SKU, price as decimal, summary, description, image path), `cart`, `checkout`, `orderConfirmation` (singleton pages under root, no properties beyond title). Content model, templates and catalogue are uSync 18 files in `src/uShopDemo.Web/uSync/v18`, imported on first boot (`uSync:Settings:ImportOnFirstBoot`). They were generated once from the former code seeder via `ExportOnSave`. `ExportOnSave` is now off (`None`): a backoffice save in a layout worktree must not rewrite `uSync/**` (layout branches differ only in views and static assets). To change content deliberately, edit in the backoffice and run an explicit uSync export, on `main` only (ADR-0017). Alternative: code seeder via `IContentService` — replaced, because content changes needed C# edits and could not be round-tripped from the backoffice. Images are static SVG/PNG files in `wwwroot/img` referenced by path, not Umbraco media (no media upload code, layout branches can restyle freely).

**Cart storage: EF Core in the Umbraco database.** `ShopDbContext` registered with Umbraco's EF Core integration (`AddUmbracoDbContext<ShopDbContext>(... UseUmbracoDatabaseProvider ...)`), sharing the Umbraco connection. Entities:
- `Cart`: `Id` (Guid, also the cookie value), `CreatedUtc`, `Status` (Open/Ordered), `OrderNumber`, `CustomerName`, `Email`, `Address`, `OrderedUtc`, `Total`.
- `CartLine`: `Id`, `CartId`, `ProductKey` (Umbraco content Guid), `ProductName`, `Sku`, `UnitPrice`, `Quantity`.
Tables prefixed `ushop` to sit clearly beside Umbraco's. Unit price and name captured at add time. Fake card number is validated for presence only and never stored. Cart cookie: HttpOnly, Secure, SameSite=Lax.
Alternatives: session cart (not persisted, user asked for DB), orders as Umbraco content (mixes transactional data with content, slower), raw NPoco/Umbraco migrations (EF Core requested).

**Migrations.** Code-first, `Migrations/` in `uShopDemo.Web`, generated with `dotnet ef` from a local tool manifest. Applied via `Database.MigrateAsync()` in a startup notification handler that runs after Umbraco install (`UmbracoApplicationStartedNotification`); the uSync import does not touch shop tables, so their order is irrelevant. Accepted gap: that notification may fire after the server accepts requests, so a request in the first seconds can hit missing tables; `add-cart-api` moves migrations to the API and applies them before it listens. EF design-time uses an `IDesignTimeDbContextFactory` with a dummy SQL Server connection string so `dotnet ef migrations add` works without a running stack.

**Cart and checkout endpoints.** A `SurfaceController` (`CartSurfaceController`) handles POSTs: add, update quantity, remove, checkout. A small `CartService` encapsulates cookie handling and EF access; views get the current cart via an injected service (`@inject ICartService`). Cart/checkout/confirmation are Umbraco content pages so they get URLs and templates like every other page; confirmation reads the order number from TempData or query. Anti-forgery via Umbraco's form helpers.

**Error pages.** 404: `notFound` document type, template `notFound` (`Views/notFound.cshtml`, uses `_Layout`) and a uSync content node under the root, set as the 404 page via `Umbraco:CMS:Content:Error404Collection`. 500: `UseExceptionHandler` with a handler that sets 500 and sends `wwwroot/errors/500.html`, registered before `UseUmbraco()` in every environment (no developer exception page for site paths). Other 4xx: `UseStatusCodePages` with a handler that sends `wwwroot/errors/4xx.html` and keeps the original status, for responses without a body. Both are skipped for `/umbraco/*`. Path re-execution is not used: the static file middleware would reset the status to 200. Static pages in `wwwroot/errors/` use `site.css`; they cannot read the branch, so they omit the badge. `CartSurfaceController.Add` returns `BadRequest()` for a product key that is not a published product. Alternatives: Razor/Umbraco 500 page (can fail on the same fault it reports), static 404 (no nav or badge, layout branches edit it separately). See ADR-0015.

**Layouts live in `Views/` and `wwwroot/`.** Contract for layout branches: `wwwroot/errors/500.html`, `wwwroot/errors/4xx.html` and `Views/notFound.cshtml` keep their paths (Program.cs and the `notFound` template reference them); restyle, do not rename or move. `Shared/_Layout.cshtml`, `shop.cshtml`, `category.cshtml`, `product.cshtml`, `cart.cshtml`, `checkout.cshtml`, `orderConfirmation.cshtml`, `notFound.cshtml`, `wwwroot/errors/*.html`, partials under `Views/Partials/`, `wwwroot/css/site.css`. Branch badge reads `USHOP_BRANCH`. No demo notice banner (ADR-0019).

**Themes.** Three: the current light look on `demo`, `theme/dark` and `theme/high-contrast`. Finished themes are built on `theme/dark-complete` and `theme/high-contrast-complete` from one `main` commit. `demo` is cut from the same commit and removes `openspec/` and `.claude/`, and replaces `CLAUDE.md` with `docs/demo-claude.md`; prompts are in `docs/demo.md` (ADR-0032, ADR-0033). A theme edits `wwwroot/css/site.css`: the `:root` colour variables, the hero gradient and the input colours, which are hard-coded; the branch badge stays as it is. Optional font and radius changes stay in CSS. Hooks need no change, but the `[hidden]` rule must stay (`docs/nav-hooks.md`). A saved CSS edit is served without a restart and the `asp-append-version` hash changes (tested in a throwaway clone). Stage flow: at the start of the talk, from `demo`, run `up` and start three instances (about 70 seconds each); one `claude` session per worktree edits only `site.css`; refresh the tabs. Alternatives and rationale: ADR-0031, ADR-0032.

**Telemetry.** `uShopDemo.ServiceDefaults` configures logs, traces and metrics for the site and API, exporting over OTLP only when Aspire supplies the endpoint. Umbraco logs through Serilog, so site logs use `Serilog.Sinks.OpenTelemetry`; API logs use the OpenTelemetry logging provider (ADR-0034).

**Scripts.** Plain ESM Node, no dependencies. `scripts/lib/ports.mjs`, `scripts/lib/git.mjs` (branch via `symbolic-ref`, works before first commit), `dev.mjs`, `worktrees.mjs` (`up|list|down [branch...]`). Worktrees created as siblings `../<repo>.<branch-slug>`; `up` creates missing branches from the branch checked out in the main worktree (`demo` on stage).

## Risks / Trade-offs

- [`System.Security.Cryptography.Pkcs` 10.0.12 pulled transitively into the AppHost has advisories GHSA-846f-f764-76mx (high) and GHSA-2fmv-rv2c-59x7 (moderate)] → Pinned to a patched version in the AppHost project (Product Owner approved 2026-11-02); remove the pin when Aspire ships a patched dependency.
- [Cold start slow (SQL boot + install + migrate + uSync import ~1-2 min), three at once heavier] → Pre-start before the talk; docs checklist.
- [Umbraco EF Core integration API differs in v18] → Task 3.3 verifies registration and migrations first; fall back to plain `AddDbContext` with the Umbraco connection string if needed.
- [Docker memory with three SQL Servers (~2 GB each)] → Document Docker memory ≥ 8 GB.
- [A migration added in one worktree only affects that worktree's DB] → Intended; call it out in the talk as a benefit.
- [Port probe race] → Acceptable; relaunch is stable.
- [Agent stalls on stage] → `git restore --source=theme/<name>-complete` restores the finished theme without a restart (ADR-0032).
- [A rehearsal's theme branch would be reused by `up`] → Delete the branches after every rehearsal; docs checklist.
- [Project rules could stall a stage run] → `demo` removes them; the rehearsal in `ushop-demo` task 5.5 confirms no prompts stall the agents.
- [Dashboard anonymous access] → HTTPS transport, but no login token; localhost demo only, documented.
- [Templates rewritten by Umbraco on creation (BOM)] → Content identical; `.gitattributes`/editor ignore BOM; verify `git status` clean after a run.
