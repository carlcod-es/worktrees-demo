# ADR-0031: CSS-only themes made live from a `demo` branch

- Status: Accepted; finished-theme and prompt location clauses superseded by ADR-0032
- Date: 2026-11-04
- Source: Product Owner decision during `ushop-demo` planning revision
- Supersedes: ADR-0007 (clause naming branches `layout/minimal`, `layout/catalog` and `layout/showcase`, branched from `main`, only)

## Context

The talk needs each variant made within a short time on stage. Different page structures across views can't be built and checked that quickly, and view edits need a restart (ADR-0007). `site.css` takes nearly all its colours from `:root` variables. Tested in a throwaway clone with the stack running: a saved `site.css` edit was served at once, without a restart, and the page's `?v=` hash changed. Starting an instance takes about 70 seconds.

## Decision

Show three themes: the current light look on `demo`, plus `theme/dark` and `theme/high-contrast`, whose diffs touch only CSS. `demo` is cut from `main` once `main` needs no more changes, and holds the agent prompt and hints, which aren't on `main`. At the start of the talk the Product Owner creates two worktrees from `demo` and starts all three instances; agents then edit the CSS in the two worktrees. Finished themes aren't kept as backups.

## Alternatives

- Three different page structures (`layout/*`): more striking, too slow to make live.
- Monochrome or a separate light theme: replaced by dark and high contrast, with the current look as the light theme.
- Committed `theme/*` backups, restored with `git restore`: rejected by the Product Owner to keep to one set of branches.
- Creating and starting everything when the themes are made: makes the audience wait about 70 seconds.

## Consequences

- The tabs differ in colour and small styling, not in page structure.
- `scripts/worktrees.mjs up` must branch from the checked-out branch, and its defaults become the two theme branches.
- No fallback if an agent stalls on stage, other than re-running it or editing by hand.
- A theme branch left over from a rehearsal would be reused by `up`, so it must be deleted before the talk.
- `demo` diverges from `main` by the prompt and hints, so later `main` changes must be merged into it.
