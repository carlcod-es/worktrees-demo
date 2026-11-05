# uShop: git worktrees demo

Run several branches of one app side by side, each from its own git worktree with its own ports and database.

The app is a small Umbraco 18 shop (catalogue, cart, dummy checkout) orchestrated by .NET Aspire, with a throwaway SQL Server container per instance. Theme branches differ only in CSS, so you can compare them in three browser tabs.

Each instance runs two services: the Umbraco site and the cart API (`src/uShopDemo.Api`, ASP.NET Core minimal API). Both use one SQL Server database, `umbracoDbDSN`; site tables are Umbraco's, API tables are `ushop*`. The browser calls the API directly.

Demo only: no payments, nothing is charged or shipped.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 13.6+: `dotnet tool install -g Aspire.Cli` (required: it supplies the dashboard and orchestrator)
- Docker, with at least 8 GB memory if running three instances (each SQL Server uses ~2 GB)
- Node.js 20+
- git 2.23+
- Claude Code CLI (`claude`), signed in: needed only for the agent flow in the talk
- A trusted ASP.NET dev certificate: `dotnet dev-certs https --trust` (the site and API are HTTPS-only; the backoffice login needs it). The certificate covers the API port too, so nothing extra needs trusting

Works on Windows, macOS and Linux.

## Run one instance

Pull the SQL Server image once (~1.5 GB):

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest
```

The first start takes 1-2 minutes: it builds, starts SQL Server, applies the API's migrations, installs Umbraco, applies its migrations and imports the content model and catalogue from uSync. The site waits for the API to report healthy. Stop with Ctrl+C; the SQL container is removed.

Backoffice login for every instance: `/umbraco`, `admin@demo.local` / `Demo-Password-1234!`.

### Option A: the launcher (used for the demo)

```sh
node scripts/dev.mjs
```

Prints the site, backoffice, API and dashboard URLs before starting. Ports are stable per worktree path, and the dashboard opens without a login. Exit code is 130 after Ctrl+C, 143 after SIGTERM and 129 after a hang-up (macOS/Linux), so scripts can tell an interrupted run from a clean one. (macOS/Linux) Without a terminal (CI, IDE task, piped output) the launcher forwards signals to the child's process group; in a terminal, and on Windows, Ctrl+C reaches the whole stack directly. A second Ctrl+C makes the launcher exit at once: the AppHost can't clean up, so the SQL container (`ushop-sql-<suffix>`) may be left running; remove it with `docker rm -f <name>` (`docker ps` lists it).

### Option B: the AppHost on its own

```sh
aspire run
```

Run it from the repo root; it finds `src/uShopDemo.AppHost`. This uses Aspire's defaults:

- The dashboard is HTTPS on a random port. Open it through the link with the login token that `aspire run` prints.
- The site gets a random port each run. Find it on the `ushop-web` resource in the dashboard, or with `aspire describe ushop-web`. The API also gets a random port, shown as the `ushop-api` resource.
- A second `aspire run` in the same directory stops the first instance. To run several at once, use one worktree each.

## Run three themes side by side

From the `demo` branch, which shows the current look:

```sh
node scripts/worktrees.mjs up      # creates ../<repo>.theme-dark and ../<repo>.theme-high-contrast
node scripts/worktrees.mjs list    # branch, site URL, API URL, dashboard URL, path per worktree
```

Start `node scripts/dev.mjs` in each directory, in its own terminal. A saved CSS edit shows on refresh, without a restart.

For the full talk flow, prompts and teardown, see [docs/demo.md](docs/demo.md). For what worktrees share, see [docs/how-worktrees-work.md](docs/how-worktrees-work.md).

## How isolation works

- **Files**: each worktree has its own checkout and HEAD, sharing one `.git` object store.
- **Ports**: `scripts/dev.mjs` derives a port block (15000-15999) from the worktree path, so the same worktree gets the same URLs every run. If a port is taken it moves to the next free block. Every endpoint (site, API, dashboard, OTLP, resource service) is HTTPS; the dev certificate is issued to `localhost` without a port, so one certificate covers every worktree.
- **Database**: every AppHost run starts a new SQL Server container with no volume, one database per instance, shared by the site and the API. Stopping the launcher removes it; the next start is a clean install.
- **Content**: the document types, templates and catalogue are uSync files in `src/uShopDemo.Web/uSync/v18`, imported into each fresh database on first boot (`ImportOnFirstBoot`).
- **Schema**: the API's EF Core migrations (history in `__ushopMigrationsHistory`) are applied when the API starts, so a migration added on one branch affects only that worktree's database.
- **Cookies**: the API port is in the session cookie name, so instances on one `localhost` don't clash. `localStorage` is per origin, so carts don't mix.

## How the cart works

- Writes (add, update, remove) are validated, queued in-process and answered 202 with an operation id. The page polls the operation, then re-reads the cart.
- Checkout returns an order number at once. The order reads `pending`, then `confirmed` or `rejected`; the confirmation page polls it.
- Sessions: a signed HttpOnly cookie `__Host-ushop-session-<apiPort>`, 30 days. A cart belongs to the session that first wrote it; other sessions see it as empty and get 404 on writes, orders and operations.
- CORS is limited to the site origin.
- API docs (Development only, no session needed): `<API URL>/scalar` (Scalar UI) and `<API URL>/openapi/v1.json` (OpenAPI document).

## Project structure

```
src/uShopDemo.AppHost/        Aspire topology: SQL Server, database, api, web
src/uShopDemo.Api/            cart API: endpoints, queue and worker, sessions, EF Core context and migrations
src/uShopDemo.Web/            Umbraco site
  Shop/                       Money.cs (price formatting), Content/ (heading normaliser, ADR-0025)
  uSync/v18/                  content model and catalogue (uSync export)
  Views/, wwwroot/            markup and static assets; themes change only wwwroot/css/site.css
  wwwroot/js/cart.js          cart client
  wwwroot/js/gallery.js       product gallery client
  wwwroot/js/nav.js           mobile menu toggle
  wwwroot/media/              seeded media (content, under ushop/)
tests/uShopDemo.Api.Tests/    API tests (in-process, SQLite)
scripts/                      dev.mjs (launcher), worktrees.mjs (up|list|down)
```

When changing views, keep the data-attribute hooks that `cart.js`, `gallery.js` and `nav.js` bind to: see [docs/cart-hooks.md](docs/cart-hooks.md), [docs/gallery-hooks.md](docs/gallery-hooks.md) and [docs/nav-hooks.md](docs/nav-hooks.md). View changes are compiled at build, so restart the launcher to see them. CSS changes need no restart.

To change content or document types: on `main` only, edit in the backoffice, then run an export from the uSync dashboard (Settings > uSync > Export) and commit `uSync/v18`. Saves are not exported automatically (`ExportOnSave` is off, ADR-0017), so unexported edits are lost on the next fresh start. Theme worktrees never write `uSync/**`. Editing the `.config` files by hand also works.

Product descriptions are built from blocks (rich text, feature list, quote). For the block types and their markup, see [docs/content-blocks.md](docs/content-blocks.md) (ADR-0023, ADR-0024, ADR-0025).

The blog, "The Daily Grind", is at `/the-daily-grind/`: a paginated listing, articles built from the same blocks, and one author. For its content model, editor workflow and markup, see [docs/blog.md](docs/blog.md).

Seeded images are SVG media: uSync items in `uSync/v18/Media`, files committed under `src/uShopDemo.Web/wwwroot/media/ushop/`. Change them on `main` like other content and commit both; a backoffice upload lands in an ignored folder, so move it under `media/ushop/` and update its `umbracoFile` path before committing (ADR-0020).

## Documentation

- [docs/requirements.md](docs/requirements.md): what the app must do
- [docs/implementation-notes.md](docs/implementation-notes.md): detail and findings the code doesn't explain
- [docs/adrs/](docs/adrs/README.md): architecture decisions
- [docs/follow-ups.md](docs/follow-ups.md): known non-blocking items

## Troubleshooting

- **Stuck on startup**: check Docker is running and has enough memory; open the dashboard URL for per-resource logs.
- **Ports busy**: the launcher shifts automatically and prints the URLs it picked.
- **Add an API migration**: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/uShopDemo.Api`.
- **"Could not reach the cart service"**: the API is down or unhealthy. Check `aspire describe` and the `ushop-api` resource logs.
- **Run the API tests**: `dotnet test` (in-process, SQLite, no Docker).
- **Certificate warning or backoffice login loop**: run `dotnet dev-certs https --trust` and restart the browser.
- The dashboard uses HTTPS but allows anonymous access (no login token). Don't expose these ports.

## Demo limitations

- Prices, names and SKUs come from the browser and are stored as given, with no catalogue lookup. A real shop would look them up server-side.
- The queue and the operation/pending-order status are in memory, so they are lost if the API stops. After a restart, an order number that was never processed reads as not found.
- Data Protection keys are stored unencrypted in `ushopDataProtectionKeys`. Anyone who can read the database could forge sessions, but could already read every cart.
- Cart and checkout need JavaScript.
- Prices display in GBP (`£1,234.56`), but the site language stays en-US.
