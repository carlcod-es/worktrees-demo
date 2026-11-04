## Why

Lightning-talk demo of git worktrees and why they matter for parallel agent work. Needs a realistic app where three variants of one demo shop run side by side, each fully isolated (ports, database), so the audience sees the payoff visually: three themes, three browser tabs, one repo.

## What Changes

- New .NET 10 solution `uShopDemo`: Aspire AppHost (`uShopDemo.AppHost`) + Umbraco 18 website (`uShopDemo.Web`) + SQL Server container, all defined in the AppHost.
- Umbraco installs unattended and seeds a catalogue on first boot: shop root, ~4 categories, ~12 products (name, SKU, price, summary, description, image). A fresh worktree needs zero manual setup.
- Shop pages: home, category listing, product detail, cart, checkout, order confirmation. Demo only: no payment, clearly labelled.
- Cart and orders stored in EF Core tables inside the Umbraco database; migrations applied automatically on startup.
- Each AppHost instance is self-contained: dynamic container names, no data volume, own SQL port, own database. Two or more AppHosts run concurrently without collisions.
- Cross-platform Node launcher (`scripts/dev.mjs`) derives stable, collision-free ports (dashboard, OTLP, resource service, web) per worktree and starts the AppHost.
- Node helper scripts to create/list/remove demo worktrees (`scripts/worktrees.mjs`).
- Branches: `main` (baseline), `theme/dark-complete` and `theme/high-contrast-complete` (finished themes, the stage fallback), `demo` (cut from `main` without OpenSpec tooling or project rules; the on-stage starting point, showing the current look as the light theme), and `theme/dark`, `theme/high-contrast`, created from `demo` during the talk and differing only in CSS, so they can be made live (ADR-0031, ADR-0032).
- Concise demo docs: setup, manual flow (2 worktrees plus the main checkout), agent flow (one prompt per theme, in `docs/demo.md`), teardown.

## Capabilities

### New Capabilities
- `aspire-orchestration`: AppHost topology (SQL Server, database, Umbraco) and per-instance isolation of containers, ports and data.
- `ushop-site`: Umbraco demo shop with unattended install, seeded catalogue, cookie-keyed cart persisted via EF Core, dummy checkout, and presentation confined to swappable views/CSS.
- `worktree-launcher`: Node scripts for deterministic per-worktree ports, starting the AppHost, and creating/removing demo worktrees.
- `layout-variants`: The three themes (`demo` plus two theme branches), the constraint that theme branches differ only in CSS, and the high-contrast requirements for low-vision users.
- `demo-guide`: Documentation of the talk flow (manual and agent-driven) and prerequisites.

### Modified Capabilities
<!-- None: greenfield repository, openspec/specs is empty. -->

## Impact

- New: `src/uShopDemo.AppHost`, `src/uShopDemo.Web` (including `Migrations/`), `scripts/`, `docs/`, `global.json`, `.gitignore`, `uShopDemo.slnx`, `.config/dotnet-tools.json` (`dotnet-ef`).
- Packages: `Umbraco.Cms` 18.3.0, `Umbraco.Cms.Persistence.EFCore`, `Microsoft.EntityFrameworkCore.Design` (dev only), `Aspire.Hosting.SqlServer`.
- Existing uncommitted `Blog.*` implementation from the earlier apply is renamed and reworked during apply.
- Requirements on the presenter machine: .NET 10 SDK, Docker, Aspire CLI (optional), Node 20+, git 2.5+. SQL Server image `mcr.microsoft.com/mssql/server:2022-latest`.
- No commits are created by tooling; commits are left to the user.
