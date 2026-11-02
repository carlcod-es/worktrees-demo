# ADR-0008: Cart stored with EF Core in the Umbraco database

- Status: Superseded by ADR-0010
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/design.md`

## Context

The shop needs a persistent cart and orders. The Product Owner asked for database storage and EF Core.

## Decision

`ShopDbContext` registered through Umbraco's EF Core integration (`AddUmbracoDbContext` with `UseUmbracoDatabaseProvider`), sharing the Umbraco connection. Tables `ushopCart` and `ushopCartLine`, prefixed to sit beside Umbraco's. Code-first migrations in `src/uShopDemo.Web/Migrations`, generated with `dotnet ef` from a local tool manifest, applied with `MigrateAsync()` in a startup notification handler after Umbraco install. A design-time factory with a dummy connection string lets `dotnet ef` run without the stack.

## Alternatives

- Session cart: not persisted; database storage was requested.
- Orders as Umbraco content: mixes transactional data with content, slower.
- Raw NPoco or Umbraco migrations: EF Core was requested.

## Consequences

- Cart data and Umbraco content share a database and a process.
- Depends on the Umbraco 18 EF Core integration API; fallback was plain `AddDbContext` with the Umbraco connection string.
