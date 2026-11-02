# ADR-0010: Separate cart API service with its own database

- Status: Accepted; database part superseded by ADR-0016
- Date: 2026-11-02 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/add-cart-api/design.md`
- Supersedes: ADR-0008

## Context

The demo needs a second, independently hosted service so each worktree runs a realistic multi-service stack. Cart writes should not make the browser wait on the database.

## Decision

New project `src/uShopDemo.Api`: ASP.NET Core minimal API on .NET 10, no Umbraco dependency, its own Aspire resource and HTTPS port (ADR-0004 offset 4). It owns database `shopdb` on the instance's SQL Server container, through `Aspire.Microsoft.EntityFrameworkCore.SqlServer` (`AddSqlServerDbContext<CartDbContext>("shopdb")`). Tables `Carts` and `CartLines` (no prefix; the database is the API's alone). A fresh `Initial` migration is applied with `MigrateAsync()` before `Run()`, so the API doesn't listen until the schema is current. `/health` passes only when migrated and the database is reachable; the AppHost starts the site only after it passes. The site's cart code, EF Core packages and migrations are removed. Product name, SKU and unit price are taken from the caller and stored as given.

## Alternatives

- Plain `AddDbContext` with `GetConnectionString`: works, but repeats what the Aspire integration provides (connection wiring, health check).

## Consequences

- Site and API are independent processes; if the API stops, the site keeps running and the browser shows an error.
- Each instance gets its own API, API database and port; isolation is unchanged.
- Caller-supplied prices are a demo simplification, noted in code and README; a real shop would look them up server-side.
- The site does not start while the API is unhealthy (intended).
