# ADR-0032: Plain `demo` branch, separate Claude sessions and finished-theme fallback

- Status: Accepted; clause removing `CLAUDE.md` superseded by ADR-0033
- Date: 2026-11-04
- Source: Product Owner decision during `ushop-demo` planning revision
- Supersedes: ADR-0031 (clauses on keeping no finished themes, and on the prompt and hints living only on `demo`)

## Context

ADR-0031 left no fallback if an agent stalls on stage. Project process on the stage branch (`CLAUDE.md` roles, approvals and review loop, OpenSpec skills, subagents) could also stall or slow an agent. Stopping an instance recreates its database, and a start takes about 70 seconds, but a CSS edit needs no restart.

## Decision

- The themes are first built and reviewed through OpenSpec on `theme/dark-complete` and `theme/high-contrast-complete`, cut from the same `main` commit. They stay in the repository as the stage fallback, restored with `git restore --source=<branch> -- src/uShopDemo.Web/wwwroot/css/site.css`.
- `demo` is cut from `main` and removes `openspec/`, `.claude/` and `CLAUDE.md`. `main` keeps them.
- Stage notes and prompts are in `docs/demo.md`. Each prompt includes contrast-checked CSS values and doesn't refer to the `-complete` branches.
- On stage, one visible `claude` session runs per worktree, with `--permission-mode acceptEdits`. Agents edit only `site.css` and don't build, run or commit.
- The presenter starts all three instances at the start of the talk and stops them at the end.

## Alternatives

- Keep OpenSpec and project rules on `demo`: risks stalling an agent on stage.
- Remove them from `main`: loses the specs and process for later work.
- One agent with subagents: less visible, and needs the orchestration the plain branch removes.
- Prompts refer to the finished branches: the agent would copy the file instead of visibly doing the work.
- Agents start and stop instances: another failure point, and a build can clash with a running instance.
- Prompts on `demo` only (ADR-0031): the Product Owner wants all stage notes in `docs/demo.md`.

## Consequences

- If an agent stalls, one command restores its theme with no restart.
- `demo` and the `-complete` branches must be re-cut or merged if `main` changes.
- The prompts and palettes are on `main`, and must match the finished themes.
- Claude needs network on stage; the fallback covers its loss.
