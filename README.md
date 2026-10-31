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

Works on Windows, macOS and Linux.

## Run one instance

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest   # once, ~1.5 GB
node scripts/dev.mjs
```

The launcher prints the site, backoffice and dashboard URLs, then runs until Ctrl+C. The first start takes 1-2 minutes: it builds, starts SQL Server, installs Umbraco, applies migrations and seeds the catalogue.

| | |
|---|---|
| Site | `http://localhost:<port>` (printed) |
| Backoffice | `/umbraco`, `admin@demo.local` / `Demo-Password-1234!` |
| Dashboard | printed, no login |

## Run three layouts side by side

```sh
node scripts/worktrees.mjs up      # creates ../<repo>.layout-minimal, -catalog, -showcase
node scripts/worktrees.mjs list    # branch, site URL, dashboard URL, path per worktree
```

Then, in a separate terminal for each worktree:

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
- **Ports**: `scripts/dev.mjs` derives a port block (15000-15999) from the worktree path, so the same worktree gets the same URLs every run. If a port is taken it moves to the next free block.
- **Database**: every AppHost run starts a new SQL Server container with no volume. Stopping the launcher removes it; the next start is a clean install.
- **Schema**: shop tables (`ushopCart`, `ushopCartLine`) are EF Core migrations applied on startup, so a migration added on one branch affects only that worktree's database.

## Layout

```
src/uShopDemo.AppHost/   Aspire topology: SQL Server, database, web
src/uShopDemo.Web/       Umbraco site
  Shop/                  cart service, surface controller, EF Core context, seeder
  Migrations/            EF Core migrations
  Views/, wwwroot/       everything a layout branch may change
scripts/                 dev.mjs (launcher), worktrees.mjs (up|list|down)
```

To change a layout, edit only `Views/` and `wwwroot/`. View changes are compiled at build, so restart the launcher to see them.

## Troubleshooting

- **Stuck on startup**: check Docker is running and has enough memory; open the dashboard URL for per-resource logs.
- **Ports busy**: the launcher shifts automatically and prints the URLs it picked.
- **Add a migration**: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/uShopDemo.Web --output-dir Migrations`.
- The dashboard runs unsecured on localhost. Don't expose these ports.
