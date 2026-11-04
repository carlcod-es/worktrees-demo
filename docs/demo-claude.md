# uShop worktrees demo

One demo shop run side by side from several git worktrees, each with its own ports and database. Stack: Umbraco 18 site, ASP.NET Core cart API, SQL Server container, all orchestrated by .NET Aspire.

`demo` shows the current light look. `theme/dark` and `theme/high-contrast` are worktrees that restyle the same shop.

## Theme work

On a `theme/*` branch:

- Edit only `src/uShopDemo.Web/wwwroot/css/site.css`. The site serves it directly, so a saved edit shows on the next refresh.
- The presenter runs the site from this directory. Don't build, start, stop or restart anything: a build can clash with the running instance.
- Don't commit, create branches or touch other worktrees.
- If the prompt gives CSS values, use them as given. No other files need reading.
- Don't use the `*-complete` branches or `docs/demo.md`. They hold the finished themes and the presenter's notes.
- Keep the `[hidden]` rule and every selector the scripts rely on. Change colours and small styling, not page structure.
- The hero gradient and input colours are hard-coded outside `:root`. Leave the branch badge as it is.
- Contrast: dark theme text at least 4.5:1. High-contrast text at least 7:1, with borders, controls and focus outlines at least 3:1.
- When finished, say so in one line.

## Other work

```sh
node scripts/dev.mjs                  # run this worktree's stack; prints its URLs
node scripts/worktrees.mjs up         # create the theme worktrees from the current branch
node scripts/worktrees.mjs list       # show each worktree's URLs
node scripts/worktrees.mjs down       # remove the worktrees, keep the branches
npm test                              # launcher tests
dotnet test                           # API tests; needs no Docker
```

- Each run starts with an empty database, seeded from `src/uShopDemo.Web/uSync/v18`, so backoffice changes are lost unless exported.
- View changes need a restart. CSS changes don't.
- When changing views, keep the data-attribute hooks: `docs/cart-hooks.md`, `docs/gallery-hooks.md`, `docs/nav-hooks.md`.
- Keep scripts cross-platform and free of dependencies, using Node built-ins only.

## Docs

- `README.md`: running the app, project structure and limitations
- `docs/requirements.md`: what the app must do
- `docs/implementation-notes.md`: detail and findings the code doesn't explain
- `docs/how-worktrees-work.md`: what worktrees share and what they don't
- `docs/adrs/`: decisions
