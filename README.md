# uShop: git worktrees demo

Run several branches of one app side by side, each from its own git worktree with its own ports and database.

The app is a small Umbraco 18 shop (catalogue, cart, dummy checkout) orchestrated by .NET Aspire, with a throwaway SQL Server container per instance. Layout branches differ only in views and CSS, so you can compare them in three browser tabs.

Demo only: no payments, nothing is charged or shipped.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 13.6+: `dotnet tool install -g Aspire.Cli` (required: it supplies the dashboard and orchestrator)
- Docker, with at least 8 GB memory if running three instances (each SQL Server uses ~2 GB)
- Node.js 20+
- git 2.5+
- A trusted ASP.NET dev certificate: `dotnet dev-certs https --trust` (the site is HTTPS-only; the backoffice login needs it)

Works on Windows, macOS and Linux.

## Run one instance

Pull the SQL Server image once (~1.5 GB):

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest
```

The first start takes 1-2 minutes: it builds, starts SQL Server, installs Umbraco, applies migrations and imports the content model and catalogue from uSync. Stop with Ctrl+C; the SQL container is removed.

Backoffice login for every instance: `/umbraco`, `admin@demo.local` / `Demo-Password-1234!`.

### Option A: the launcher (used for the demo)

```sh
node scripts/dev.mjs
```

Prints the site, backoffice and dashboard URLs before starting. Ports are stable per worktree path, and the dashboard opens without a login.

### Option B: the AppHost on its own

```sh
aspire run
```

Run it from the repo root; it finds `src/uShopDemo.AppHost`. This uses Aspire's defaults:

- The dashboard is HTTPS on a random port. Open it through the link with the login token that `aspire run` prints.
- The site gets a random port each run. Find it on the `web` resource in the dashboard, or with `aspire describe web`.
- A second `aspire run` in the same directory stops the first instance. To run several at once, use one worktree each.

## Run three layouts side by side

```sh
node scripts/worktrees.mjs up      # creates ../<repo>.layout-minimal, -catalog, -showcase
node scripts/worktrees.mjs list    # branch, site URL, dashboard URL, path per worktree
```

Then, in a separate terminal for each worktree (or use `aspire run` there instead):

```sh
cd ../<repo>.layout-minimal
node scripts/dev.mjs
```

Each instance shows its branch in a badge in the bottom-right corner. Place an order in one: it exists only in that instance's database.

Teardown: stop each launcher with Ctrl+C, then

```sh
node scripts/worktrees.mjs down    # removes the worktree directories, keeps the branches
```

## How isolation works

- **Files**: each worktree has its own checkout and HEAD, sharing one `.git` object store.
- **Ports**: `scripts/dev.mjs` derives a port block (15000-15999) from the worktree path, so the same worktree gets the same URLs every run. If a port is taken it moves to the next free block. Every endpoint (site, dashboard, OTLP, resource service) is HTTPS; the dev certificate is issued to `localhost` without a port, so one certificate covers every worktree.
- **Database**: every AppHost run starts a new SQL Server container with no volume. Stopping the launcher removes it; the next start is a clean install.
- **Content**: the document types, templates and catalogue are uSync files in `src/uShopDemo.Web/uSync/v18`, imported into each fresh database on first boot (`ImportOnFirstBoot`).
- **Schema**: shop tables (`ushopCart`, `ushopCartLine`) are EF Core migrations applied on startup, so a migration added on one branch affects only that worktree's database.

## Layout

```
src/uShopDemo.AppHost/   Aspire topology: SQL Server, database, web
src/uShopDemo.Web/       Umbraco site
  Shop/                  cart service, surface controller, EF Core context, startup migrations
  uSync/v18/             content model and catalogue (uSync export)
  Migrations/            EF Core migrations
  Views/, wwwroot/       everything a layout branch may change
scripts/                 dev.mjs (launcher), worktrees.mjs (up|list|down)
```

To change a layout, edit only `Views/` and `wwwroot/`. View changes are compiled at build, so restart the launcher to see them.

To change content or document types: edit in the backoffice. uSync writes each save to `uSync/v18` (`ExportOnSave`); commit those files. The next fresh start imports them. Editing the `.config` files by hand also works.

## Troubleshooting

- **Stuck on startup**: check Docker is running and has enough memory; open the dashboard URL for per-resource logs.
- **Ports busy**: the launcher shifts automatically and prints the URLs it picked.
- **Add a migration**: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/uShopDemo.Web --output-dir Migrations`.
- **Certificate warning or backoffice login loop**: run `dotnet dev-certs https --trust` and restart the browser.
- The dashboard uses HTTPS but allows anonymous access (no login token). Don't expose these ports.
