# Development

Working on the app. To run it, see `README.md`; to run the talk, see `docs/demo.md`.

## Launcher

`node scripts/dev.mjs` prints the URLs before starting. Ports are stable per worktree path, and the dashboard opens without a login. It uses HTTPS but allows anonymous access, so don't expose these ports.

- **Exit codes:** 130 after Ctrl+C, 143 after SIGTERM and 129 after a hang-up (macOS/Linux), so scripts can tell an interrupted run from a clean one.
- **Signals (macOS/Linux):** without a terminal (CI, IDE task, piped output), the launcher forwards signals to the child's process group. In a terminal, and on Windows, Ctrl+C reaches the whole stack directly.
- **Second Ctrl+C:** the launcher exits at once and the AppHost can't clean up, so the SQL container (`ushop-sql-<suffix>`) may be left running. Remove it with `docker rm -f <name>` (`docker ps` lists it).

## AppHost without the launcher

Run `aspire run` from the repo root; it finds `src/uShopDemo.AppHost`. This uses Aspire's defaults:

- The dashboard is HTTPS on a random port. Open it through the link with the login token that `aspire run` prints.
- The site and API get random ports each run. Find them on the `ushop-web` and `ushop-api` resources in the dashboard, or with `aspire describe ushop-web`.
- A second `aspire run` in the same directory stops the first instance. To run several at once, use one worktree each.

## API

- Docs (Development only, no session needed): `<API URL>/scalar` (Scalar UI) and `<API URL>/openapi/v1.json` (OpenAPI document).
- Add a migration: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/uShopDemo.Api`.

## Views and CSS

View changes are compiled at build, so restart the launcher to see them. CSS changes need no restart.

Keep the data-attribute hooks that the scripts bind to: `docs/cart-hooks.md`, `docs/gallery-hooks.md`, `docs/nav-hooks.md`.

## Content and media

Make these changes on `main` only. Theme worktrees never write `uSync/**`.

- **Content and document types:** edit in the backoffice, run an export from the uSync dashboard (Settings > uSync > Export), and commit `uSync/v18`. Saves aren't exported automatically (`ExportOnSave` is off, ADR-0017), so unexported edits are lost on the next fresh start. Editing the `.config` files by hand also works.
- **Seeded images:** SVG media, as uSync items in `uSync/v18/Media` with files under `src/uShopDemo.Web/wwwroot/media/ushop/`. Commit both. A backoffice upload lands in an ignored folder, so move it under `media/ushop/` and update its `umbracoFile` path before committing (ADR-0020).
- **Product descriptions:** see `docs/content-blocks.md` (ADR-0023, ADR-0024, ADR-0025).
- **Blog:** "The Daily Grind", at `/the-daily-grind/`. See `docs/blog.md`.

## Troubleshooting

- **Stuck on startup:** check Docker is running and has enough memory. Open the dashboard URL for per-resource logs.
- **Ports busy:** the launcher shifts automatically and prints the URLs it picked.
- **"Could not reach the cart service":** the API is down or unhealthy. Check `aspire describe` and the `ushop-api` resource logs.
- **Certificate warning or backoffice login loop:** run `dotnet dev-certs https --trust` and restart the browser.
