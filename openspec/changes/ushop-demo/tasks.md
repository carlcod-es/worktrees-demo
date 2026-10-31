Note: an earlier apply pass produced uncommitted `Blog.*` code (see design.md Context). Tasks marked "(carry over)" are expected to be largely satisfied by renaming that code; verify before checking off.

## 1. Repository foundation

- [ ] 1.1 Rename `Blog.*` to `uShopDemo.*` (directories, csproj, `uShopDemo.slnx`, namespaces, `Projects.uShopDemo_Web`), `BLOG_*` env vars to `USHOP_*` in AppHost, views and scripts; verify `dotnet build uShopDemo.slnx` succeeds and `grep -ri blog src scripts` finds nothing
- [ ] 1.2 (carry over) `.gitignore`, `global.json`, `package.json`, no AppHost `launchSettings.json`; add `.config/dotnet-tools.json` with `dotnet-ef`; verify `dotnet tool restore` succeeds and `git status` shows only intended files

## 2. AppHost

- [ ] 2.1 (carry over) AppHost: SQL Server (no volume), `umbracoDbDSN` database, `web` project with reference/WaitFor, provider name env, `USHOP_WEB_PORT`, `USHOP_BRANCH`; verify the site responds on the configured port and `docker ps` shows one `sql-*` container for the instance
- [ ] 2.2 Verify two AppHosts started from different directories run concurrently with separate SQL containers and separate databases

## 3. Umbraco shop

- [ ] 3.1 (carry over) `Program.cs` and `appsettings.json` (unattended install, `ModelsMode: Nothing`, URL detection), views compiled at build; verify backoffice login with demo credentials after first boot
- [ ] 3.2 Replace blog seeder with catalogue seeder: document types `shop`, `category`, `product`, `cart`, `checkout`, `orderConfirmation`; templates from on-disk views; `TemplateId` on every node; 4 categories, 12 products with SKU/price/summary/description/image; product images in `wwwroot/img`; verify home lists categories, every product page returns 200, restart against same DB creates no duplicates
- [ ] 3.3 Add `Umbraco.Cms.Persistence.EFCore`, `ShopDbContext` (`Cart`, `CartLine`, `ushop` table prefix), design-time factory, initial migration via `dotnet ef migrations add Initial`, startup handler that migrates before seeding; verify `__EFMigrationsHistory` and `ushop*` tables exist in a fresh DB
- [ ] 3.4 `CartService` (cookie id, get/create cart, add/update/remove, totals, place order with generated order number) and `CartSurfaceController` (add, update, remove, checkout with validation); verify add/remove/checkout via the browser and that an ordered cart row has status Ordered and customer fields set
- [ ] 3.5 Baseline views on `main`: `_Layout` (nav of categories, cart count, demo notice, branch badge), `shop`, `category`, `product`, `cart`, `checkout`, `orderConfirmation`, `site.css`; verify full flow (browse → add → cart → checkout → confirmation) at 1280x720 and that no shop logic lives in views beyond rendering

## 4. Launcher and worktree scripts

- [ ] 4.1 (carry over) `scripts/lib/ports.mjs` and `scripts/ports.test.mjs`; verify `npm test` passes
- [ ] 4.2 (carry over) `scripts/dev.mjs` with `USHOP_*` vars; verify two worktrees started from separate terminals get distinct, repeatable URLs
- [ ] 4.3 `scripts/worktrees.mjs` `up`, `list`, `down`; verify the cycle once `main` has a commit (requires the user to commit the baseline)

## 5. Layout branches

- [ ] 5.1 Create `layout/minimal` (clean single column, text-first) in a worktree; verify full flow works and `git diff --name-only main` lists only views/CSS/static assets
- [ ] 5.2 Create `layout/catalog` (dense grid, category sidebar); same verification
- [ ] 5.3 Create `layout/showcase` (large imagery, editorial hero); same verification
- [ ] 5.4 Run all three worktrees concurrently via the launcher; verify three distinct shops with correct branch badges, and that an order placed in one is absent from the others' databases

## 6. Demo docs

- [ ] 6.1 Write `README.md` (what, prerequisites, quick start, demo credentials) and `docs/demo.md` (pre-talk checklist incl. image pre-pull and pre-build, manual flow, teardown); verify commands run as written
- [ ] 6.2 Write `docs/agent-prompt.md` with the paste-ready three-shop-layouts prompt and constraints; verify by dry-running with an agent and confirming three branches and three running instances
- [ ] 6.3 Write `docs/how-worktrees-work.md` one-pager (shared `.git`, per-worktree HEAD/index, per-worktree DB and migrations, commands used in the talk); verify it fits on one screen
