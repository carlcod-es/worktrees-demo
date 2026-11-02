# ADR-0014: API tests in process with SQLite in-memory

- Status: Accepted
- Date: 2026-11-02 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/add-cart-api/design.md`

## Context

The cart API's behaviour (validation, ordering, checkout lifecycle, CORS, sessions and ownership) needs automated tests that run quickly and without Docker.

## Decision

`tests/uShopDemo.Api.Tests`: xUnit with `Microsoft.AspNetCore.Mvc.Testing`, hosting the API through `WebApplicationFactory` with `CartDbContext` swapped for SQLite in-memory on one shared open connection. The API uses `EnsureCreatedAsync()` on SQLite because the migrations are SQL Server-specific. Each test client keeps its own cookie container, so two clients act as two browsers.

## Alternatives

- Testcontainers SQL Server: more faithful, but adds about 20 s per run and Docker in tests.

## Consequences

- Fast tests with no external dependencies.
- The SQL Server path and migrations are not covered by automated tests; manual end-to-end verification (`add-cart-api` task 6.1) covers them.
