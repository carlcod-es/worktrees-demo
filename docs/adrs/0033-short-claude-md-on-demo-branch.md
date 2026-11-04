# ADR-0033: Short `CLAUDE.md` on the `demo` branch

- Status: Accepted
- Date: 2026-11-04
- Source: Product Owner decision during `ushop-demo` planning
- Supersedes: ADR-0032 (clause that `demo` removes `CLAUDE.md`, only; `demo` still removes the project process)

## Context

ADR-0032 removes the project process from `demo` so it can't stall stage agents. With no `CLAUDE.md`, an agent has only the pasted prompt and has to explore to learn the repo. Anyone reading `demo` afterwards gets no orientation either.

## Decision

`demo` replaces `CLAUDE.md` with a short one describing the repo, the theme rules and the docs. It's kept on `main` as `docs/demo-claude.md`, and moved to `CLAUDE.md` when `demo` is cut, so `demo` has one copy. It has no roles, approvals or review loop.

## Alternatives

- No `CLAUDE.md`: the prompt alone carries every rule, and the repo has no orientation.
- `AGENTS.md` with a `CLAUDE.md` that imports it: useful for other tools, but only Claude runs on stage, and it adds a second file for no stage benefit.
- Trim the project `CLAUDE.md`: mixes stage rules with process that `demo` doesn't have.

## Consequences

- Stage rules are in both the prompts and `CLAUDE.md`, so they must change together.
- `docs/demo-claude.md` has no effect on `main`, because Claude Code only auto-loads files named `CLAUDE.md`.
- The rehearsal in `ushop-demo` task 5.5 has to confirm it doesn't slow or stall the agents.
