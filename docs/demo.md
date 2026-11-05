# Running the demo

One shop, three themes, three browser tabs, one repo. Two Claude sessions restyle the site in parallel worktrees while it runs.

| Tab | Branch | Directory | Theme |
|---|---|---|---|
| 1 | `demo` | `worktrees-demo` | Current look (light) |
| 2 | `theme/dark` | `worktrees-demo.theme-dark` | Dark |
| 3 | `theme/high-contrast` | `worktrees-demo.theme-high-contrast` | High contrast |

Paths assume the repo directory is `worktrees-demo`. Prerequisites are in `README.md`.

**Why no restart:** the app serves `wwwroot/css/site.css` directly from the worktree, so a saved edit shows on the next refresh. The AppHost and database keep running.

## After the final slice

Do this once `ushop-demo` tasks 5.1, 5.2, 5.3, 6.1 and 6.2 are done and committed, including the two `-complete` branches.

**1. Check `main`**

```sh
git switch main
git status                        # clean
npm test
dotnet test
```

**2. Check the finished themes**

Task 5.3 cut the `-complete` branches from `main`, and later commits (task 6.2, task ticks) moved `main` on. After the last `main` commit, rebase both onto it and return to `main`. A rebase rewrites the branches, so if they're already pushed, push them with `git push --force-with-lease origin theme/dark-complete theme/high-contrast-complete`.

```sh
git rebase main theme/dark-complete
git rebase main theme/high-contrast-complete
git switch main
```

Each diff must list only `site.css`, and all three commands must print the same commit.

```sh
git diff --name-only main...theme/dark-complete
git diff --name-only main...theme/high-contrast-complete
git rev-parse main
git merge-base main theme/dark-complete
git merge-base main theme/high-contrast-complete
```

**3. Cut `demo`**

This removes OpenSpec and the project process, and puts the short `CLAUDE.md` in place (ADR-0032, ADR-0033).

```sh
git switch -c demo main
git rm -r -q openspec .claude CLAUDE.md
git mv docs/demo-claude.md CLAUDE.md
git commit -m "Prepare demo branch for the stage run"
```

**4. Check `demo`**

The first command must list only `CLAUDE.md` and `docs/demo-claude.md`. The second must print nothing.

```sh
git diff --name-only main demo -- . ":!openspec" ":!.claude"
git ls-tree --name-only demo openspec .claude
```

Steps 1 to 4 and 8 run on the machine you prepare on. Steps 6 and 7 run on the Mac you'll present from.

**5. Push the branches**

The presenting Mac gets everything from here, so this isn't optional.

```sh
git push origin main demo theme/dark-complete theme/high-contrast-complete
```

**6. Set up the presenting Mac**

Install the prerequisites in `README.md`, sign in to `claude`, then clone. The fallback needs the `-complete` branches as local branches, and a clone only creates the checked-out one.

```sh
git clone <repo-url> worktrees-demo
cd worktrees-demo
git branch theme/dark-complete origin/theme/dark-complete
git branch theme/high-contrast-complete origin/theme/high-contrast-complete
git switch demo
```

**7. Rehearse**

Follow [Before the talk](#before-the-talk) and then [On stage](#on-stage) in full. Check:

- [ ] No agent stops for a permission prompt or asks a question.
- [ ] Agents edit only `site.css` and don't build, run or commit anything.
- [ ] Each theme looks like its `-complete` branch.
- [ ] Branch badges differ, and an order in one tab doesn't appear in the others.
- [ ] Both themes work through the full flow. High contrast is usable at 200% zoom.
- [ ] The fallback in [If something goes wrong](#if-something-goes-wrong) works with no restart.
- [ ] You've noted how long the three instances take to start.

Reset as described in [Before the talk](#before-the-talk) step 8.

**8. Close the plan on `main`**

On the machine you prepare on, tell Claude the rehearsal passed, so it can tick `ushop-demo` tasks 5.4 and 5.5. Then archive, commit and push:

```sh
git switch main
openspec archive ushop-demo --skip-specs
git add openspec
git commit -m "Archive ushop-demo"
git push origin main
```

`demo` doesn't need this commit, because it only touches `openspec/`.

If the rehearsal finds a problem, fix it on the machine you prepare on, and commit to `main`. Merge `main` into each `-complete` branch (`git switch <branch>`, then `git merge --no-edit main`). Then merge it into `demo` with the commands below, which keep the short `CLAUDE.md` and leave out the project process, whatever `main` changed. On `demo`, conflict messages from the merge are expected, including after the step 8 archive commit.

```sh
git switch demo
git merge --no-commit --no-ff main
git rm -r -f -q --ignore-unmatch openspec .claude docs/demo-claude.md
git checkout HEAD -- CLAUDE.md
git commit -m "Merge main into demo"
git switch main
```

A change to `docs/demo-claude.md` doesn't carry over, so apply it to `CLAUDE.md` on `demo` by hand.

Then repeat from step 1, without the step 2 rebase and without step 3: `demo` already exists, and the merges keep the `-complete` branches based on `main`, so the step 2 checks still hold. Push with step 5, then pull all three on the presenting Mac. Check the two theme diffs still list only `site.css`.

## Before the talk

Needs network. Run these on the Mac you'll present from, at the real repo path: ports come from the path, so the bookmarks stay valid on stage.

1. `docker pull mcr.microsoft.com/mssql/server:2022-latest`
2. `dotnet dev-certs https --trust`, then restart the browser.
3. In Docker Desktop, under Settings > Resources, give Docker at least 8 GB of memory (each SQL Server uses ~2 GB). On Apple Silicon, the SQL Server image is `linux/amd64` and runs under emulation, so it starts more slowly and needs Docker's Rosetta or QEMU emulation left on.
4. Check `claude` is signed in. It's the only thing that needs network on stage, so test the venue connection or have a hotspot ready.
5. Stop anything else using the stack: `docker ps --filter name=ushop-sql` should list nothing.
6. `git switch demo`, then `dotnet build uShopDemo.slnx`.
7. Rehearse the whole of [On stage](#on-stage). This also fills the NuGet cache, accepts any Claude folder-trust prompts for the worktree paths, and lets you bookmark the three site URLs.
8. Reset after the rehearsal. **Do this after every run:** `up` reuses a leftover branch, and the theme would already be applied.
   ```sh
   node scripts/worktrees.mjs down --force
   git branch -D theme/dark theme/high-contrast
   ```

On the day, set up the screen before going on:

- 5 terminals, large font, all in `worktrees-demo` on `demo`: 3 for instances, 2 for Claude.
- Browser with the 3 bookmarked URLs, signed in nowhere that matters.
- Do Not Disturb on, power connected, and `caffeinate -d` running in a spare tab so the Mac doesn't sleep.
- This page and the two prompts open somewhere you can copy from, off the shared screen.
- `git status` clean, no `theme/*` worktrees, `docker ps --filter name=ushop-sql` empty.

## On stage

**1. Create the worktrees (start of the talk)**

```sh
node scripts/worktrees.mjs up     # branches and worktrees for theme/dark, theme/high-contrast
git worktree list                 # one repo, three checkouts
```

**2. Start all three instances**, one per terminal. Each takes a minute or more, and the worktrees also build first, so talk through this and carry on:

```sh
node scripts/dev.mjs
```
```sh
cd ../worktrees-demo.theme-dark && node scripts/dev.mjs
```
```sh
cd ../worktrees-demo.theme-high-contrast && node scripts/dev.mjs
```

**3. Open the three tabs.** Same look in all of them; the badge at the bottom right shows each branch.

**4. Show the isolation.** Add a product to the cart in one tab and check out. The cart count in the other two doesn't change.

```sh
docker ps --filter name=ushop-sql    # one database container per instance
```

**5. Start the agents**, one per terminal, and paste the matching prompt into each:

```sh
cd ../worktrees-demo.theme-dark && claude --permission-mode acceptEdits -n dark
```
```sh
cd ../worktrees-demo.theme-high-contrast && claude --permission-mode acceptEdits -n high-contrast
```

`acceptEdits` stops file edits asking for approval. The prompts need no shell commands, so nothing else should prompt.

**6. Refresh each tab as its agent finishes.** The theme changes with no restart, and the order and cart from step 4 are still there.

**7. Show the diff is CSS only:**

```sh
git -C ../worktrees-demo.theme-dark status --short
git -C ../worktrees-demo.theme-high-contrast status --short
```

Both list only `site.css`. The changes are uncommitted, so `git diff demo...theme/dark` shows nothing here.

**8. Shut down.** Press Ctrl+C once in each instance terminal and wait. A second Ctrl+C leaves the SQL container running.

```sh
node scripts/worktrees.mjs down --force
docker ps --filter name=ushop-sql    # should list nothing
```

`--force` is needed because the theme edits are uncommitted, and it discards them. Delete the theme branches before the next run (`git branch -D theme/dark theme/high-contrast`).

## Prompts

Paste as is. The values are contrast-checked: at least 4.5:1 for dark text, and at least 7:1 for high-contrast text and 3:1 for its borders and controls.

### Dark

````text
This is a git worktree on branch theme/dark, and the site is already running from this directory.

Apply a dark theme by editing src/uShopDemo.Web/wwwroot/css/site.css:

1. In the existing :root rule, change only these values, and keep --radius and font-size:
   color-scheme: dark;
   --fg: #f2f4f7; --muted: #b4bcc8; --bg: #14171c; --card: #1f242c; --line: #3d4551;
   --accent: #f4a672; --accent-fg: #14171c; --notice: #3a2f12; --notice-fg: #ffe08a; --error: #ff9c94;

2. Add this at the end of the file:
   .hero { background: linear-gradient(135deg, #2a1a12, #7a3a1e); }
   input, textarea { background: var(--bg); border-color: #8a93a1; }

Edit only that file. Don't build, start or stop anything, don't run any commands, and don't commit. When you've finished, say so in one line.
````

### High contrast

````text
This is a git worktree on branch theme/high-contrast, and the site is already running from this directory.

Apply a high-contrast theme for low-vision users by editing src/uShopDemo.Web/wwwroot/css/site.css:

1. In the existing :root rule, change only these values, and keep --radius and font-size:
   color-scheme: dark;
   --fg: #fff; --muted: #fff; --bg: #000; --card: #000; --line: #fff;
   --accent: #ffe600; --accent-fg: #000; --notice: #000; --notice-fg: #fff; --error: #ffb3ad;

2. Add this at the end of the file:
   a:not(.button) { text-decoration: underline !important; }
   .hero { background: #000; border: 2px solid #fff; }
   .hero p { opacity: 1; }
   input, textarea { background: #000; border: 2px solid #fff; }
   :focus-visible { outline: 3px solid #ffe600; outline-offset: 2px; }
   .cart-status:not(:empty) { border: 2px solid #fff; font-weight: 700; }
   .field-error:not(:empty)::before { content: "Error: "; font-weight: 700; }
   .input-validation-error, [aria-invalid="true"] { border-width: 4px; border-style: dashed; }
   button[aria-busy="true"], button:disabled { opacity: 1; }

Edit only that file. Don't build, start or stop anything, don't run any commands, and don't commit. When you've finished, say so in one line.
````

## If something goes wrong

| Problem | Do this |
|---|---|
| Agent stalls, or no network | Restore the finished theme from inside that worktree, then refresh (no restart): `git restore --source=theme/dark-complete -- src/uShopDemo.Web/wwwroot/css/site.css` (or `theme/high-contrast-complete`) |
| Agent wants to build or run something | Deny it and tell it to edit the CSS only. A build can clash with the running instance. |
| Theme doesn't show | Hard refresh. Check the file changed with `git status --short` in that worktree. |
| Instance slow to start | Carry on with the others; its tab catches up. Check the dashboard URL in its terminal. |
| Instance won't start | Check Docker is running and has the memory. Ctrl+C once, then `node scripts/dev.mjs` again; it starts with a new database. |
| Theme already applied on `up` | A rehearsal branch was left behind. Run `down --force`, then `git branch -D` both theme branches, then `up`. |
| SQL container left behind | `docker rm -f <name>` |

## See also

`docs/how-worktrees-work.md`: what worktrees share and what they don't.
