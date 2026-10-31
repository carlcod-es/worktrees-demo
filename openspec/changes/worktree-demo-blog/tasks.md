## 1. Repository foundation

- [ ] 1.1 Add `.gitignore` (bin, obj, `.vs`, Umbraco `umbraco/Data`, `wwwroot/media`, `node_modules`), keep `global.json`, create `Blog.slnx` with both projects; verify `dotnet build` succeeds
- [ ] 1.2 Reconcile existing uncommitted scaffold (`src/Blog.AppHost`, `src/Blog.Web`) with design.md; remove AppHost `launchSettings.json` port pins; verify `git status` shows only intended files

## 2. AppHost

- [ ] 2.1 Implement `src/Blog.AppHost/AppHost.cs`: SQL Server (no volume), `umbracoDbDSN` database, `web` project with reference/WaitFor, dynamic SQL port; verify `dotnet run` starts SQL and web resources in the dashboard
- [ ] 2.2 Add configurable `BLOG_WEB_PORT`, unattended-install env, provider name, `BLOG_BRANCH` env; verify the site responds on the configured port
- [ ] 2.3 Verify two AppHosts started with different ports run concurrently with separate SQL containers (`docker ps`) and separate databases

## 3. Umbraco site and seeding

- [ ] 3.1 Minimal `Blog.Web` `Program.cs` with Umbraco, appsettings (unattended defaults, runtime mode, no telemetry prompts); verify backoffice login with demo credentials after first boot
- [ ] 3.2 Spike v18 service APIs and implement idempotent seeder (document types Blog and Blog Post, templates from on-disk views, five posts with images/author/date); verify home page lists five posts and a restart creates no duplicates
- [ ] 3.3 Baseline layout on `main`: `_Layout.cshtml`, `Blog.cshtml`, `BlogPost.cshtml`, `wwwroot/css/site.css`, branch badge; verify every page shows the badge and no layout-specific logic exists outside views/CSS

## 4. Launcher and worktree scripts

- [ ] 4.1 Write `scripts/lib/ports.mjs` (path hash → block, bind probe, shift on conflict) and a node:test unit test for stability and conflict shifting; verify `node --test scripts` passes
- [ ] 4.2 Write `scripts/dev.mjs` (detect branch, set env, print URLs, run AppHost, forward SIGINT); verify two worktrees started from separate terminals get distinct, repeatable URLs
- [ ] 4.3 Write `scripts/worktrees.mjs` with `up`, `list`, `down` (sibling dirs, create missing branches from `main`, keep branches on down); verify the cycle on a scratch run

## 5. Layout branches

- [ ] 5.1 Create `layout/minimal` (single narrow serif column) in a worktree; verify it renders and `git diff --name-only main` lists only views/CSS
- [ ] 5.2 Create `layout/magazine` (hero post plus card grid); same verification
- [ ] 5.3 Create `layout/sidebar` (two-column with sidebar of recent posts and tags); same verification
- [ ] 5.4 Run all three worktrees concurrently via the launcher and verify three distinct sites with correct branch badges at 1280x720

## 6. Demo docs

- [ ] 6.1 Write `README.md` (what, prerequisites, quick start) and `docs/demo.md` (pre-talk checklist including image pre-pull, manual flow, teardown); verify commands in the docs run as written
- [ ] 6.2 Write `docs/agent-prompt.md` with the paste-ready three-layout prompt and constraints; verify by dry-running it with an agent and confirming three branches and three running instances
- [ ] 6.3 Add a `docs/how-worktrees-work.md` one-pager (shared `.git`, per-worktree HEAD/index, commands used in the talk); verify it fits on one screen
