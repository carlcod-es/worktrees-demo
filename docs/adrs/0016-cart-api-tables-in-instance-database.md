# ADR-0016: Cart API tables in the instance's single database

- Status: Accepted
- Date: 2026-11-02
- Source: `openspec/changes/add-cart-api/design.md`
- Supersedes: ADR-0010 (database part only), ADR-0012 (key storage location only)

## Context

ADR-0010 gave the cart API its own database, `shopdb`, beside Umbraco's `umbracoDbDSN` in each instance's SQL Server container. The Product Owner does not want two databases in the app. The shop's EF Core migrations only create custom cart tables and do not depend on Umbraco, so they still belong to the API.

## Decision

- One database per instance (`umbracoDbDSN`), shared by the site and the cart API.
- The API connects with `AddSqlServerDbContext<CartDbContext>("umbracoDbDSN")`; the AppHost references and waits for the same database resource. No `shopdb`.
- The API owns `ushop`-prefixed tables: `ushopCart`, `ushopCartLine`, `ushopDataProtectionKeys`. Its migrations history is `__ushopMigrationsHistory`.
- The API reads and writes only `ushop*` tables; the site never touches them. The API keeps no Umbraco package dependency, applies its own migrations before `Run()`, and keeps its `/health` check.
- The site still waits for the API (`web.WaitFor(api)`), so the API's tables exist before Umbraco's unattended install. This must be verified (add-cart-api task 4.2); if the install fails, the fallback order `api.WaitFor(web)` goes to the Product Owner first.

## Alternatives

- Separate `shopdb` (ADR-0010): cleanest separation, rejected by the Product Owner.
- API migrates after Umbraco installs (`api.WaitFor(web)`): avoids the install-order risk, but drops the "site starts only after the API is healthy" requirement. Held as fallback.
- Default `__EFMigrationsHistory`: may be shared with Umbraco's own EF Core history; a dedicated table avoids collisions.

## Consequences

- One database to inspect per instance; isolation between worktrees is unchanged (one container each).
- Site and API share a schema namespace; the prefix and ownership rule keep them apart, enforced by review, not by the database.
- Losing the database loses Umbraco content, carts and Data Protection keys together; every run starts empty anyway.
