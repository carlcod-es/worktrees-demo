Note (2026-11-03): this change's specs were synced to `openspec/specs` before `add-cart-api` was archived on top of them. Archive this change with `openspec archive ushop-demo --skip-specs`; a normal archive would overwrite the `add-cart-api` modifications. If a spec delta here changes before archive, sync it by hand first.

Note: an earlier apply pass produced uncommitted `Blog.*` code (see design.md Context). Tasks marked "(carry over)" are expected to be largely satisfied by renaming that code; verify before checking off.

## 1. Repository foundation

- [x] 1.1 Rename `Blog.*` to `uShopDemo.*` (directories, csproj, `uShopDemo.slnx`, namespaces, `Projects.uShopDemo_Web`), `BLOG_*` env vars to `USHOP_*` in AppHost, views and scripts; verify `dotnet build uShopDemo.slnx` succeeds and `grep -ri blog src scripts` finds nothing
- [x] 1.2 (carry over) `.gitignore`, `global.json`, `package.json`, no AppHost `launchSettings.json`; add `.config/dotnet-tools.json` with `dotnet-ef`; verify `dotnet tool restore` succeeds and `git status` shows only intended files

## 2. AppHost

- [x] 2.1 (carry over) AppHost: SQL Server (no volume), `umbracoDbDSN` database, `web` project with reference/WaitFor, provider name env, `USHOP_WEB_PORT`, `USHOP_BRANCH`; verify the site responds on the configured port and `docker ps` shows one `sql-*` container for the instance
- [x] 2.2 Verify two AppHosts started from different directories run concurrently with separate SQL containers and separate databases

## 3. Umbraco shop

- [x] 3.1 (carry over) `Program.cs` and `appsettings.json` (unattended install, `ModelsMode: Nothing`, URL detection), views compiled at build; verify backoffice login with demo credentials after first boot
- [x] 3.2 Content model, templates and catalogue as uSync 18 files in `src/uShopDemo.Web/uSync/v18`, imported on first boot (ADR-0006): document types `shop`, `category`, `product`, `cart`, `checkout`, `orderConfirmation`; templates from on-disk views; template assigned on every node; 4 categories, 12 products with SKU/price/summary/description/image; product images in `wwwroot/img`; verify home lists categories, every product page returns 200, restart against same DB creates no duplicates
- [x] 3.3 Add `Umbraco.Cms.Persistence.EFCore`, `ShopDbContext` (`Cart`, `CartLine`, `ushop` table prefix), design-time factory, initial migration via `dotnet ef migrations add Initial`, startup handler that migrates after Umbraco install; verify `__EFMigrationsHistory` and `ushop*` tables exist in a fresh DB
- [x] 3.4 `CartService` (cookie id, get/create cart, add/update/remove, totals, place order with generated order number) and `CartSurfaceController` (add, update, remove, checkout with validation); verify add/remove/checkout via the browser and that an ordered cart row has status Ordered and customer fields set
- [x] 3.5 Baseline views on `main`: `_Layout` (nav of categories, cart count, demo notice, branch badge), `shop`, `category`, `product`, `cart`, `checkout`, `orderConfirmation`, `site.css`; verify full flow (browse → add → cart → checkout → confirmation) at 1280x720 and that no shop logic lives in views beyond rendering
- [x] 3.6 Error page markup (worker-frontend): `Views/notFound.cshtml` using `_Layout` (heading, short message, link home), `wwwroot/errors/500.html` and `wwwroot/errors/4xx.html` (standalone HTML, `/css/site.css`, demo notice, link home, no script); verify `dotnet build uShopDemo.slnx` succeeds and both static pages render styled when opened at `/errors/500.html` and `/errors/4xx.html`
- [x] 3.7 Error handling wiring (worker-backend; explicit assignment of `appsettings.json` and `uSync/**`): `notFound` document type, template and published content node under the root as uSync files; `Umbraco:CMS:Content:Error404Collection` pointing at it; `UseExceptionHandler` / `UseStatusCodePages` handlers before `UseUmbraco()` that send `wwwroot/errors/500.html` / `4xx.html` with the original status, in every environment, skipping `/umbraco/*` and responses with a body; `CartSurfaceController.Add` returns 400 for a key that is not a published product; cart cookie `Secure = true`; depends on 3.6; verify `dotnet build uShopDemo.slnx` succeeds
- [x] 3.9 Review follow-ups, front end (worker-frontend): product image `alt` = product title; product card image link hidden from assistive tech (`aria-hidden="true" tabindex="-1"`); cart link `aria-label` with item count; checkout field errors linked to inputs with `aria-describedby`; comment that the product description is trusted editor HTML; branch badge read through `IConfiguration` instead of `Environment`; error-page button left-aligned; every text size in `site.css` at least 18px (demo notice, cart count, summaries, intros, field errors, SKU, footer); verify `dotnet build uShopDemo.slnx` succeeds and no rendered text is below 18px at 1280x720
- [x] 3.10 Review follow-ups, back end (worker-backend): `PlaceOrderAsync` succeeds at most once per cart (conditional update on `Status == Open`, second submit gets the existing order or a validation error, never a new order number) and retries once on an order-number unique-index collision; depends on 3.7; verify `dotnet build uShopDemo.slnx` succeeds
- [x] 3.8 Verify, after 3.7, 3.9 and 3.10, on a fresh `node scripts/dev.mjs` run in Firefox: the full flow from 3.5 still works; double-clicking Place order yields one order; `/no-such-page` returns 404 with nav, demo notice and branch badge; an add-to-cart post with a random product key returns 400 with the 4xx page and the cart count is unchanged; stopping the `sql` resource in the Aspire dashboard then loading a page returns 500 with the static page and no stack trace; backoffice login still works; devtools shows the cart cookie as HttpOnly and Secure
- [x] 3.11 CSS cache-busting (worker-frontend): `asp-append-version="true"` on the `site.css` link in `_Layout.cshtml`; verify `dotnet build uShopDemo.slnx` succeeds and the rendered link carries a `?v=` hash
- [x] 3.12 uSync export off (worker-backend; explicit assignment of `appsettings.json`): `uSync:Settings:ExportOnSave` = `None`; verify `dotnet build uShopDemo.slnx` succeeds, and on a fresh run a backoffice save of a content node leaves `git status` clean under `uSync/`
- [x] 3.13 Patch `System.Security.Cryptography.Pkcs` advisories (worker-backend; explicit assignment of `src/uShopDemo.AppHost/uShopDemo.AppHost.csproj`): pin the lowest patched version as a direct package reference in the AppHost; verify `dotnet build uShopDemo.slnx` has no NU1902/NU1903 warnings and `dotnet list uShopDemo.slnx package --vulnerable --include-transitive` reports none
- [x] 3.14 `worktrees.mjs` review leftovers (worker-backend): detect a dirty worktree with `git -C <dir> status --porcelain` instead of matching git's English error text; `isInside` uses `rel === ".." || rel.startsWith(".." + path.sep)`; lowercase paths only on Windows and macOS in `normPath`, matching `slotFor`, or document why not; remove stray blank lines; verify `npm test` passes
- [x] 3.15 (added by Product Owner decision, 2026-11-02; ADR-0019) Remove the demo notice banner (worker-frontend): the `.demo-notice` element from `Views/Shared/_Layout.cshtml`, `wwwroot/errors/500.html` and `wwwroot/errors/4xx.html`, and the `.demo-notice` rule from `site.css` (keep `--notice`/`--notice-fg`, used by `.cart-status`); verify `dotnet build uShopDemo.slnx` succeeds and `grep -rn "demo-notice\|no real orders" src/uShopDemo.Web --include=*.cshtml --include=*.html --include=*.css` finds nothing

## 4. Launcher and worktree scripts

- [x] 4.1 (carry over) `scripts/lib/ports.mjs` and `scripts/ports.test.mjs`; verify `npm test` passes
- [x] 4.2 (carry over) `scripts/dev.mjs` with `USHOP_*` vars; verify two worktrees started from separate terminals get distinct, repeatable URLs
- [x] 4.3 `scripts/worktrees.mjs` `up`, `list`, `down`; verify the cycle once `main` has a commit (requires the user to commit the baseline)

## 5. Layout branches

Note (from `add-cart-api`): cart, checkout, confirmation and the header count are rendered by `wwwroot/js/cart.js` from the cart API. Layout branches 5.1-5.3 must keep the data-attribute hooks and `<template>` elements documented in `docs/cart-hooks.md`; change markup and CSS around them, not the script. Create these branches after `add-cart-api` is merged.

- [ ] 5.1 Create `layout/minimal` (clean single column, text-first) in a worktree; verify full flow works and `git diff --name-only main` lists only views/CSS/static assets
- [ ] 5.2 Create `layout/catalog` (dense grid, category sidebar); same verification
- [ ] 5.3 Create `layout/showcase` (large imagery, editorial hero); same verification
- [ ] 5.4 Run all three worktrees concurrently via the launcher; verify three distinct shops with correct branch badges, and that an order placed in one is absent from the others' databases

## 6. Demo docs

- [ ] 6.1 Write `README.md` (what, prerequisites, quick start, demo credentials) and `docs/demo.md` (pre-talk checklist incl. image pre-pull and pre-build, manual flow, teardown); verify commands run as written
- [ ] 6.2 Write `docs/agent-prompt.md` with the paste-ready three-shop-layouts prompt and constraints; verify by dry-running with an agent and confirming three branches and three running instances
- [ ] 6.3 Write `docs/how-worktrees-work.md` one-pager (shared `.git`, per-worktree HEAD/index, per-worktree DB and migrations, commands used in the talk); verify it fits on one screen
