# ADR-0017: uSync ExportOnSave off; explicit export on main only

- Status: Accepted
- Date: 2026-11-02
- Source: `openspec/changes/ushop-demo/design.md`
- Supersedes: ADR-0006 (export mechanism only; uSync files and first-boot import stand)

## Context

ADR-0006 kept content in uSync files with `ExportOnSave` on, so every backoffice save rewrote `src/uShopDemo.Web/uSync/v18`. Layout branches must differ from `main` only in `Views/` and `wwwroot/` (`layout-variants` spec, ADR-0007). A content save in a layout worktree, by a presenter or an agent, would rewrite `uSync/**` and break that rule.

## Decision

- `uSync:Settings:ExportOnSave` = `None`. `ImportOnFirstBoot` stays on.
- Deliberate content changes: on `main` only, edit in the backoffice, run an export from the uSync dashboard (Settings > uSync > Export), commit `uSync/v18`. Hand-editing the `.config` files also works.
- Layout worktrees never write `uSync/**`.

## Alternatives

- Keep `ExportOnSave` on and tell presenters not to commit uSync changes: relies on discipline; agents may commit everything.
- Turn export on only for `main` via an environment override: more moving parts for a demo.

## Consequences

- Backoffice edits are lost on the next fresh start unless exported and committed.
- `git diff --name-only main` on a layout branch stays limited to views and static assets even after backoffice use.
