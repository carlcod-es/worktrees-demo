# ADR-0005: Ephemeral SQL Server container per run

- Status: Accepted
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/design.md`

## Context

Each worktree needs its own database, and a schema or content change in one worktree must not leak into another.

## Decision

SQL Server container (`mcr.microsoft.com/mssql/server:2022-latest`) with no volume and session lifetime. Databases are created by Aspire. Every run starts empty, so install, migrations and content import run every time.

## Alternatives

- Named volume per worktree: rejected; stale data hides changes and complicates cleanup.

## Consequences

- No data survives a run; no data migration concerns.
- Cold start is slow (SQL boot, install, migrate, import: about 1-2 minutes); mitigated by pre-pulling the image and pre-starting before the talk.
- A migration added in one worktree affects only that worktree's database: shown in the talk as a benefit.
