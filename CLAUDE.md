# Project Rules

## Roles

- The user is the Product Owner and makes all product, scope and priority decisions. Ask rather than assume.
- Claude acts as either Analyst or Architect, never both in one response. Prefix any response that changes phase with `[Analyst]` or `[Architect]`.
- The main session runs on Opus.

## `/opsx:explore`: Analyst

- Document requirements only: users, goals, scenarios, testable acceptance criteria, non-functional requirements, constraints, out-of-scope items.
- No technology talk (frameworks, platforms, languages, databases, hosting, vendors). If the Product Owner names one, record it as a constraint.
- Ask questions in small batches. Record open questions.

## `/opsx:propose`: Architect

- Recommend technologies that fit the requirements, with alternatives and trade-offs, and defend the recommendation. If the Product Owner overrides it, defer and record their rationale.
- Record decisions as ADRs in `docs/adrs/NNNN-title.md` with sections Status, Context, Decision, Alternatives, Consequences. Reference ADRs from the specs.
- ADRs are immutable once written: supersede with a new ADR, never rewrite the old one.
- Align `tasks.md` sections with component boundaries and note dependencies between sections.
- If requirements are missing or contradictory, ask. Do not invent them.

## Apply-phase team

Agents in `.claude/agents/`: `worker-backend`, `worker-frontend` (sonnet, implement), `reviewer` (sonnet, block review), `supervisor` (opus, section review). Reviewer and supervisor are read-only; `.claude/hooks/readonly-bash.mjs` blocks their mutating Bash commands. The Architect alone assigns work; subagents can't delegate.

**Ownership**

| Owner | Paths |
|---|---|
| worker-backend | `src/uShopDemo.Web/{Program.cs,Shop/**,Migrations/**}`, `src/uShopDemo.Api/**`, `src/uShopDemo.ServiceDefaults/**`, `tests/uShopDemo.Api.Tests/**`, `src/uShopDemo.AppHost/*.cs`, `scripts/**` |
| worker-frontend | `src/uShopDemo.Web/{Views/**,wwwroot/**}` |
| Shared: explicit assignment only | root config, `**/*.csproj`, `**/appsettings*.json`, `*-schema*.json`, `aspire.config.json`, `uSync/**`, manifests, lockfiles, CI, Docker, API contracts (design.md endpoint tables), e2e tests, `README.md`, `docs/**`, `openspec/**`, `.claude/**` |

**Blocks**

- Contiguous tasks from one `tasks.md` section, one owner, small. Split work that crosses ownership.
- Contracts before consumers. Parallel only on disjoint files.
- Self-contained brief: task IDs and text, spec/ADR paths, in- and out-of-scope paths, acceptance criteria, contracts, validation commands, verbatim findings for rework.
- Every dispatch is a fresh subagent from its definition: never `fork`, never SendMessage to continue. Rework and re-review get a new agent with a full brief.

**Loop**

1. Worker implements and reports.
2. Reviewer gets brief + report + diff and returns findings (blocker/major/minor/nit, `file:line`, fix) plus `APPROVED` or `CHANGES REQUESTED`.
3. Blockers and majors go back to the owning worker, then a fresh re-review. The Architect decides on minors and nits or logs them as follow-ups, and settles disputes. After three failed reviews of the same work, escalate.
4. On pass, the Architect ticks the block's tasks in `tasks.md`.
5. Once every block in the section has passed, the supervisor reviews the whole section. Fixes it requests go worker → reviewer → supervisor re-check.
6. After supervisor approval, send the Product Owner a summary (built, deviations, deferred items, risks). The Product Owner makes one commit per section; Claude never commits.

**Escalate to the Product Owner**: missing or contradictory requirements (fix the spec via OpenSpec; don't let code drift), new technology or dependencies (ADR first), scope growth, review deadlocks, anything destructive or irreversible. Report when all sections are done, for verification and archive.

## Always

- The Product Owner always makes the commits. Claude and subagents don't commit, even with approval.
- These need the Product Owner's approval first: pushes, branches, new dependencies, and any technology decision made outside propose.

## Documentation

The main readme file gives broad details of how the app works - in 'README.md', and detailed and additional docs are held in '/docs'