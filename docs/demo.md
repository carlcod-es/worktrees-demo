# Running the demo

Three layouts of one shop, three browser tabs, one repo. Prerequisites are in `README.md`. Commands run from the main checkout unless they `cd`; worktrees are siblings, so each `cd` also works from another worktree. Paths assume the repo directory is `worktrees-demo`; `node scripts/worktrees.mjs list` prints the real ones.

## Before the talk

Needs network. Afterwards, nothing downloads or restores on stage.

1. `docker pull mcr.microsoft.com/mssql/server:2022-latest`
2. `dotnet dev-certs https --trust`, then restart the browser.
3. Give Docker at least 8 GB of memory (each SQL Server uses ~2 GB).
4. `node scripts/worktrees.mjs up`
5. Build each worktree:
   ```sh
   cd ../worktrees-demo.layout-minimal
   dotnet build uShopDemo.slnx
   cd ../worktrees-demo.layout-catalog
   dotnet build uShopDemo.slnx
   cd ../worktrees-demo.layout-showcase
   dotnet build uShopDemo.slnx
   ```
6. Run the manual flow once and bookmark the three site URLs; ports stay the same per worktree path. Stop with Ctrl+C, but don't run `down`: it deletes the build output from step 5.

A start takes 1-2 minutes even when pre-built, because each run installs into a new database. For no wait on stage, start all three before you go on.

## Manual flow

1. Create the worktrees, if they aren't there, and show the URLs:
   ```sh
   node scripts/worktrees.mjs up
   node scripts/worktrees.mjs list
   ```
2. In a separate terminal for each worktree:
   ```sh
   cd ../worktrees-demo.layout-minimal   # then -catalog, -showcase
   node scripts/dev.mjs
   ```
3. Open the three site URLs. The badge at the bottom right shows each branch.
4. In one site: browse a category, open a product, add to cart, check out, and show the confirmation.
5. Show the isolation:
   ```sh
   git worktree list                                 # one repo, three checkouts
   git diff --name-only main...layout/minimal        # views, CSS and assets only
   docker ps --filter name=ushop-sql                 # one database container per instance
   ```
   The cart count in the other two sites hasn't changed.

## Teardown

1. Press Ctrl+C once in each terminal and wait. A second Ctrl+C leaves the SQL container running; remove it with `docker rm -f <name>`.
2. `node scripts/worktrees.mjs down` removes the directories and keeps the branches.
3. `docker ps --filter name=ushop-sql` should list nothing, unless another instance is running.

## See also

`docs/how-worktrees-work.md`: what worktrees share and what they don't.
