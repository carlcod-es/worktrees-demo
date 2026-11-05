# How worktrees work

A worktree is an extra checkout of the same repository, in its own directory, on its own branch.

| Shared by all worktrees | Separate per worktree |
|---|---|
| `.git` object store: commits, blobs | Files on disk |
| Branches and tags | `HEAD` |
| Remotes and repo config | Index (staging area) |
| Stashes | `bin/`, `obj/`, other ignored files |

The main checkout holds `.git/`. Each linked worktree has a `.git` file pointing to `.git/worktrees/<name>`, where its `HEAD` and index live. A branch can be checked out in only one worktree at a time.

## What git doesn't isolate

Git separates the files. Two running copies of an app still collide on ports, databases and cookies, so this repo isolates those too:

| Resource | Isolation |
|---|---|
| Ports | `scripts/dev.mjs` derives a port block (15000-15999) from the worktree path, so a worktree gets the same URLs every run. If a port is taken, it moves to the next free block. Every endpoint is HTTPS, and the dev certificate is issued to `localhost` without a port, so one certificate covers every worktree. |
| Database | A new SQL Server container per run, no volume, with one database shared by the site and API. Stopping the launcher removes it, and the next start is a clean install (ADR-0005). |
| Schema | The API's EF Core migrations (history in `__ushopMigrationsHistory`) run at startup, so a migration on one branch reaches only that worktree's database. |
| Content | The files in `src/uShopDemo.Web/uSync/v18` are imported into each new database on first boot (`ImportOnFirstBoot`, ADR-0006). |
| Cookies and carts | The API port is part of the session cookie name. `localStorage` is per origin, so carts don't mix. |

## Commands used in the talk

```sh
git worktree add -b theme/dark ../worktrees-demo.theme-dark demo   # new branch, new directory
git worktree add ../worktrees-demo.theme-dark theme/dark           # existing branch
git worktree list
git worktree remove ../worktrees-demo.theme-dark                   # keeps the branch
git worktree prune                                                 # forgets directories deleted by hand
```

`node scripts/worktrees.mjs up|list|down` wraps these for the two theme branches. See `docs/demo.md`.
