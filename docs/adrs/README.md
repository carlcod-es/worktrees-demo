# Architecture decisions

ADRs are immutable: a change gets a new ADR that supersedes the old clause.

Older ADRs refer to planning files that aren't here:

- **`openspec/changes/...` in a Source line:** the planning record. The finished changes were removed in commit `a8cb918`, so read them from its parent, for example `git show a8cb918^:openspec/changes/archive/2026-11-04-add-blog/design.md`. `demo` has no `openspec/` at all.
- **A named spec, such as the `blog` spec:** see the section with that name in `docs/requirements.md`.
- **A task, such as `add-cart-api` task 6.1:** the same planning record.
