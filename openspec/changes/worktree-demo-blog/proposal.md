## Why

Lightning-talk demo of git worktrees and why they matter for parallel agent work. Needs a realistic app where three variants of one blog run side by side, each fully isolated (ports, database), so the audience sees the payoff visually: three layouts, three browser tabs, one repo.

## What Changes

- New .NET 10 solution: Aspire AppHost + Umbraco 18 website (`Blog.Web`) + SQL Server container, all defined in the AppHost.
- Umbraco installs unattended and seeds a blog (document types, templates, sample posts) on first boot, so a fresh worktree needs zero manual setup.
- Each AppHost instance is self-contained: dynamic container names, no data volume, own SQL port, own database. Two or more AppHosts run concurrently without collisions.
- Cross-platform Node launcher (`scripts/dev.mjs`) derives stable, collision-free ports (dashboard, OTLP, resource service, web) per worktree and starts the AppHost.
- Node helper scripts to create/list/remove demo worktrees (`scripts/worktrees.mjs`).
- Four git branches: `main` (baseline layout) and `layout/minimal`, `layout/magazine`, `layout/sidebar`, differing only in views/CSS, for the non-AI demo.
- Concise demo docs: setup, manual flow (3 worktrees), agent flow (prompt for 3 parallel layouts), teardown.

## Capabilities

### New Capabilities
- `aspire-orchestration`: AppHost topology (SQL Server, database, Umbraco) and per-instance isolation of containers, ports and data.
- `blog-site`: Umbraco blog with unattended install, seeded content model and sample posts, and a layout confined to swappable views/CSS.
- `worktree-launcher`: Node scripts for deterministic per-worktree ports, starting the AppHost, and creating/removing demo worktrees.
- `layout-variants`: The three layout branches and the constraint that they differ only in presentation files so they merge/compare cleanly.
- `demo-guide`: Documentation of the talk flow (manual and agent-driven) and prerequisites.

### Modified Capabilities
<!-- None: greenfield repository, openspec/specs is empty. -->

## Impact

- New: `src/Blog.AppHost`, `src/Blog.Web`, `scripts/`, `docs/`, `global.json`, `.gitignore`, solution file.
- Existing uncommitted scaffold from an interrupted start (`global.json`, `src/Blog.AppHost`, `src/Blog.Web` csproj) is a starting point and is expected to be revised during apply.
- Requirements on the presenter machine: .NET 10 SDK, Docker, Aspire CLI (optional), Node 20+, git 2.5+. SQL Server image `mcr.microsoft.com/mssql/server:2022-latest` (amd64 under emulation on Apple Silicon).
- No commits are created by tooling; branch creation is part of the demo setup, commits are left to the user.
