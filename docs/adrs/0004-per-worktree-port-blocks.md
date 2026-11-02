# ADR-0004: Per-worktree port blocks set through environment variables

- Status: Accepted
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/design.md`; extended by `openspec/changes/add-cart-api/design.md`

## Context

Several AppHosts run at once from different worktrees. Each needs stable, predictable URLs for the stage, without editing tracked files per worktree. Tooling must work on Windows, macOS and Linux.

## Decision

A dependency-free ESM Node launcher (`scripts/dev.mjs`, helpers in `scripts/lib/`) derives a port block from the SHA-1 of the real, lower-cased worktree path: slot 0..99, block = 15000 + slot × 10. Offsets: web 0, dashboard 1, OTLP 2, resource service 3, cart API 4 (added by `add-cart-api`). It probes by binding and moves to the next slot on conflict; there is no state file. Ports reach the AppHost as environment variables (`ASPNETCORE_URLS`, `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL`, `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`, `USHOP_WEB_PORT`, `USHOP_API_PORT`), and the AppHost runs with `--no-launch-profile`. `scripts/worktrees.mjs` creates, lists and removes sibling worktrees.

## Alternatives

- Per-worktree `launchSettings.json` edits: leave git state dirty.
- Fully dynamic ports: URLs change every run, which is bad on stage.

## Consequences

- The same worktree gets the same URLs every run unless its slot is taken.
- The probe can race with another process taking a port; a relaunch settles it.
- The dashboard allows anonymous access (`ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`); acceptable for a localhost demo only.
