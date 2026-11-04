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
| Ports | `scripts/dev.mjs` derives a port block from the worktree path |
| Database | A new SQL Server container per run, no volume (ADR-0005) |
| Schema | Migrations run at startup, so a migration on one branch reaches only that worktree's database |
| Content | uSync files are imported into each new database (ADR-0006) |
| Cookies | The API port is part of the session cookie name |

## Commands used in the talk

```sh
git worktree add -b layout/minimal ../worktrees-demo.layout-minimal main   # new branch, new directory
git worktree add ../worktrees-demo.layout-minimal layout/minimal           # existing branch
git worktree list
git worktree remove ../worktrees-demo.layout-minimal                       # keeps the branch
git worktree prune                                                         # forgets directories deleted by hand
```

`node scripts/worktrees.mjs up|list|down` wraps these for the three layout branches. See `docs/demo.md`.
