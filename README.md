# uShop: git worktrees demo

Demo app for a talk on git worktrees. The app is a small Umbraco 18 shop (catalogue, cart, dummy checkout, blog) orchestrated by .NET Aspire, with a throwaway SQL Server container per instance.

Each instance runs two services: the Umbraco site and the cart API (ASP.NET Core minimal API). Both use one SQL Server database; site tables are Umbraco's, API tables are `ushop*`. The browser calls the API directly.

Demo only: no payments, nothing is charged or shipped.

## What it demonstrates

- **Branches running side by side:** one repo checked out in three worktrees, one site per branch, compared in three browser tabs.
- **Isolation git doesn't provide:** each worktree gets its own ports, database and cookies ([docs/how-worktrees-work.md](docs/how-worktrees-work.md)).
- **Parallel agents:** two Claude Code sessions restyle two running sites at once, with no restart. Theme branches differ only in CSS.

To run the talk, follow [docs/demo.md](docs/demo.md). The rest of this page covers running one instance from `main`.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 13.6+: `dotnet tool install -g Aspire.Cli` (required: it supplies the dashboard and orchestrator)
- Docker, with about 2 GB of memory free for SQL Server
- Node.js 20+
- A trusted ASP.NET dev certificate: `dotnet dev-certs https --trust` (the site and API are HTTPS-only)

Works on Windows, macOS and Linux.

## Run

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest   # once, ~1.5 GB
git switch main
node scripts/dev.mjs
```

It prints the site, backoffice, API and dashboard URLs. The first start takes 1-2 minutes. Backoffice login: `/umbraco`, `admin@demo.local` / `Demo-Password-1234!`.

Stop with Ctrl+C, which removes the SQL container. Every start installs and seeds a new database.

## Test

```sh
npm test       # launcher scripts
dotnet test    # API, in-process with SQLite, no Docker
```

## Project structure

```
src/uShopDemo.AppHost/         Aspire topology: SQL Server, database, api, web
src/uShopDemo.Api/             cart API: endpoints, queue and worker, sessions, EF Core context and migrations
src/uShopDemo.ServiceDefaults/ logs, traces and metrics for the site and API
src/uShopDemo.Web/             Umbraco site
  Shop/                        server code: price formatting, blog listing, heading normaliser
  uSync/v18/                   content model and catalogue (uSync export)
  Views/, wwwroot/             markup and static assets; themes change only wwwroot/css/site.css
tests/uShopDemo.Api.Tests/     API tests
scripts/                       dev.mjs (launcher), worktrees.mjs (up|list|down)
```

## Documentation

- [docs/demo.md](docs/demo.md): running the talk, start to finish
- [docs/how-worktrees-work.md](docs/how-worktrees-work.md): what worktrees share, and how instances are isolated
- [docs/development.md](docs/development.md): launcher and AppHost detail, changing the app, troubleshooting
- [docs/requirements.md](docs/requirements.md): what the app must do
- [docs/implementation-notes.md](docs/implementation-notes.md): detail and findings the code doesn't explain
- [docs/blog.md](docs/blog.md), [docs/content-blocks.md](docs/content-blocks.md): blog and product content
- [docs/cart-hooks.md](docs/cart-hooks.md), [docs/gallery-hooks.md](docs/gallery-hooks.md), [docs/nav-hooks.md](docs/nav-hooks.md): contracts between scripts and views
- [docs/adrs/](docs/adrs/README.md): architecture decisions
- [docs/follow-ups.md](docs/follow-ups.md): known non-blocking items

## Demo limitations

- Prices, names and SKUs come from the browser and are stored as given, with no catalogue lookup. A real shop would look them up server-side.
- The queue and the operation/pending-order status are in memory, so they are lost if the API stops. After a restart, an order number that was never processed reads as not found.
- Data Protection keys are stored unencrypted in `ushopDataProtectionKeys`. Anyone who can read the database could forge sessions, but could already read every cart.
- Cart and checkout need JavaScript.
- Prices display in GBP (`£1,234.56`), but the site language stays en-US.
