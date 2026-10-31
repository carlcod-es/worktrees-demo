## Context

See `proposal.md` for motivation. Repo is empty apart from OpenSpec config. An interrupted start left an uncommitted scaffold: `global.json` (SDK 10.0.202), `src/Blog.AppHost` (Aspire SDK 13.6.1, Aspire.Hosting.SqlServer, project reference), `src/Blog.Web` csproj (Umbraco.Cms 18.3.0, net10.0). Toolchain observed: .NET 10.0.202, Aspire CLI 13.6.4, Docker 29 on arm64, Node 24. `mcr.microsoft.com/mssql/server:2022-latest` is already pulled locally.

Aspire already randomises container name suffixes and SQL host ports and proxies project endpoints on dynamic ports; the only hard-coded values are the dashboard, OTLP and resource service URLs in `launchSettings.json`.

## Goals / Non-Goals

**Goals:**
- `node scripts/dev.mjs` in any worktree yields a working, isolated, seeded site.
- Layout work touches only views/CSS, so branches diff cleanly.

**Non-Goals:**
- Production hardening, persistence across runs, auth, tests of Umbraco itself.
- Blazing-fast startup. Cold start is mitigated by pre-pulling images and pre-building, not engineered away.

## Decisions

**Port isolation via env, not launchSettings.** The launcher computes ports and sets `ASPIRE_DASHBOARD_PORT`-style values through `ASPNETCORE_URLS`, `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`, `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`, `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`, `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`, and `BLOG_WEB_PORT`, then runs `dotnet run --project src/Blog.AppHost --no-launch-profile`. Alternative: edit launchSettings per worktree — rejected, dirty git state and defeats the demo. Alternative: all-dynamic ports — rejected, unstable URLs make a live demo harder and prevent bookmarking.

**Port block derivation.** Hash of the absolute worktree path → slot 0..99; block = base 15000 + slot*10 (web +0, dashboard +1, OTLP +2, resource service +3). Probe each port by binding; on conflict increment slot. Stable for a given path, collision-safe, no state file. Alternative: central registry file — rejected, shared mutable state across worktrees.

**Database isolation by ephemerality.** SQL Server container with no volume and no `WithLifetime(Persistent)`; Aspire names it with a random suffix; the `umbracoDbDSN` database is created by Aspire. Each run is a fresh DB, which also guarantees seeding is exercised. Alternative: per-worktree named volume — rejected, stale data would hide layout/seed changes and complicate cleanup.

**Umbraco wiring.** Connection string comes from `WithReference(db)` (`ConnectionStrings__umbracoDbDSN`); provider name supplied via env. Unattended install via `Umbraco__CMS__Unattended__*` env. Fixed `Global:Id` is not required. Demo credentials are documented, not secret.

**Seeding in code.** A composer registers a notification handler (`UmbracoApplicationStartedNotification` or equivalent in v18) that, if no `blog` root exists, creates document types, templates (content read from the on-disk `.cshtml` so the Umbraco-managed file matches), and posts via the v18 services (`IContentTypeService`, `ITemplateService`, `IContentService`). Alternatives: uSync (extra package, file churn), Umbraco packages/`.udt` import (removed in recent versions). Exact v18 API surface is to be confirmed during apply; the spec constrains behavior only.

**Layouts live in `Views/` and `wwwroot/css/`.** `_Layout.cshtml`, `Blog.cshtml`, `BlogPost.cshtml`, `site.css` are the only files varying per branch. Branch badge reads `BLOG_BRANCH` env set by the launcher (`git rev-parse --abbrev-ref HEAD`). Alternative: Umbraco themes/partials per layout flag in content — rejected, the point is branch-based divergence.

**Branches.** `main` holds the baseline; `layout/*` branches are created from `main` with one commit each. Commits are made by the user per global instructions; apply prepares changes on branches in worktrees and leaves them uncommitted, or the user runs a documented one-liner. See Open Questions.

**Scripts.** Plain ESM Node, no dependencies. `scripts/lib/ports.mjs` shared; `dev.mjs` and `worktrees.mjs` (`up|list|down`). Worktrees created as siblings under `../worktrees-demo.<branch-slug>`.

**SQL on Apple Silicon.** Default SQL 2022 image runs under emulation; accepted since already working locally. Fallback `azure-sql-edge` documented, not implemented.

## Risks / Trade-offs

- [Cold start slow (SQL boot + Umbraco install ~1-2 min), three at once heavier] → Pre-start before the talk; docs checklist; keep one pre-warmed set as backup.
- [Umbraco 18 service APIs differ from memory] → Task 3 spikes seeding first; adjust before building layouts.
- [Docker memory pressure with three SQL Servers (~2 GB each)] → Document minimum Docker memory (8 GB+); option to share one SQL container deferred.
- [Port probe race between check and bind] → Acceptable for a demo; relaunch is stable.
- [Dashboard unsecured mode] → Localhost only, demo only, documented.
- [Templates DB/file sync overwriting views] → Seeder passes on-disk content when creating templates, verified in task 3.

## Open Questions

- Should apply create the three layout commits itself? Default: it prepares each layout in its worktree and leaves commits to the user; `scripts/worktrees.mjs` documents the commit commands.
