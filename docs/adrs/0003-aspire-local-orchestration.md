# ADR-0003: Aspire AppHost for local orchestration

- Status: Accepted
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/proposal.md`, `design.md`

## Context

Each worktree must run the full stack (database plus site) with one command, and several stacks must run side by side without collisions.

## Decision

Define the whole stack in an Aspire AppHost (`uShopDemo.AppHost`): SQL Server container, database(s) and projects. The site gets its connection string from `WithReference(db)` and its provider name via `ConnectionStrings__umbracoDbDSN_ProviderName`. Nothing outside the AppHost definition is needed to run the stack.

## Alternatives

None recorded in the source design.

## Consequences

- Aspire randomises container name suffixes and SQL host ports and proxies project endpoints, so only the dashboard, OTLP and resource-service ports need explicit isolation (ADR-0004).
- Requires Docker on the presenter machine; three SQL Server containers need about 6 GB, so Docker memory must be at least 8 GB.
- A backgrounded AppHost ignores SIGINT; stop it with SIGTERM so it cleans up its container.
