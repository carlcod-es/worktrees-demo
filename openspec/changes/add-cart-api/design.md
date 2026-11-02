## Context

Current state (see proposal.md - Why for motivation):

- `src/uShopDemo.Web/Shop/`: `CartService` (cookie cart id, EF Core through Umbraco's `IEFCoreScopeProvider`), `CartSurfaceController` (form posts + redirects), `ShopDbContext` (`ushopCart`, `ushopCartLine`), `ShopComposer`/`ShopStartup` (applies migrations on `UmbracoApplicationStartedNotification`). Migrations in `src/uShopDemo.Web/Migrations`.
- Views read the cart through `@inject ICartService Cart` in `_ViewImports.cshtml`: `_Layout` (header count), `cart`, `checkout`, `orderConfirmation`. `product.cshtml` posts to `CartSurfaceController.Add`. `Money.Format` lives in `CartService.cs` and is used by `product`, `productCard` and `category` views.
- AppHost: one SQL Server container (no volume), database `umbracoDbDSN`, `web` project on an HTTPS endpoint whose port comes from `USHOP_WEB_PORT`.
- `scripts/lib/ports.mjs`: 10-port block per worktree, offsets `web 0, dashboard 1, otlp 2, resourceService 3`.
- Site content comes from uSync; layout branches change only `Views/` and `wwwroot/`.
- No test project exists yet.

## Goals / Non-Goals

**Goals:**
- API is a plain ASP.NET Core app with no Umbraco dependency; it could be deployed alone.
- Layout branches keep full control of cart/checkout markup without touching the client script.
- Same per-worktree isolation as today: every instance gets its own API, database and port. One database per instance, shared by site and API with disjoint tables (ADR-0016).

**Non-Goals:**
- Durable queue, retries, or exactly-once delivery. Queued writes are lost when the API process stops.
- User accounts or login. Sessions are anonymous and per browser.
- Working without JavaScript. Cart and checkout require JS after this change.
- Product data verification (decided: trust caller, see `cart-api` spec).
- OpenTelemetry/ServiceDefaults wiring. Console logging only, as for the site today.

## Decisions

ADRs: ADR-0004, ADR-0010, ADR-0011, ADR-0012, ADR-0013, ADR-0014, ADR-0016, ADR-0018 (`docs/adrs/`). This section keeps the implementation detail.

**Project shape.** `src/uShopDemo.Api`, ASP.NET Core minimal API, .NET 10, added to `uShopDemo.slnx` and referenced by the AppHost. EF Core SQL Server via the Aspire client integration `Aspire.Microsoft.EntityFrameworkCore.SqlServer` (`AddSqlServerDbContext<CartDbContext>("umbracoDbDSN")`, the instance's single database): picks up the connection string from the AppHost reference and adds a DB health check. Alternative: plain `AddDbContext` with `GetConnectionString`: works, but repeats what the integration gives for free.

**Endpoints.**

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/session` | | 204, sets or renews the session cookie |
| GET | `/carts/{cartId}` | | 200 cart |
| POST | `/carts/{cartId}/lines` | `productKey, name, sku, unitPrice, quantity` | 202 `{ operationId }` |
| PUT | `/carts/{cartId}/lines/{productKey}` | `quantity` (0 removes) | 202 `{ operationId }` |
| DELETE | `/carts/{cartId}/lines/{productKey}` | | 202 `{ operationId }` |
| POST | `/carts/{cartId}/checkout` | `name, email, address, cardNumber` | 202 `{ operationId, orderNumber }` |
| GET | `/operations/{operationId}` | | 200 `{ status: queued\|completed\|failed, error? }` / 404 |
| GET | `/orders/{orderNumber}` | | 200 `{ status: pending\|confirmed\|rejected, ... }` / 404 |
| GET | `/health` | | 200 when DB reachable |

Every endpoint except `/session` and `/health` requires a valid session (401 otherwise). For another session's data: cart reads return empty, cart writes, operations and orders return 404. 202 responses carry `Location: /operations/{id}`. Validation failures return `ValidationProblem` (400). Lines are keyed by product key, not by DB line id: the browser knows the product key before the add is processed, and repeat adds become idempotent per product. Alternative: line ids as today, rejected because the client can't address a line until its add has been processed.

**Sessions.** `POST /session` reads the session cookie and unprotects it with an `ITimeLimitedDataProtector` (purpose `ushop.session.v1`). If the cookie is valid, the endpoint re-issues it with a fresh 30-day lifetime. If it is missing, tampered with or expired, the endpoint creates a new 128-bit id (`RandomNumberGenerator`). Cookie: `__Host-ushop-session-<port>`, HttpOnly, Secure, SameSite=Strict, Path=/, no Domain, `Max-Age` 30 days. The `__Host-` prefix makes the browser refuse the cookie unless it is Secure, host-only and path `/`.
- The port is in the name because browsers don't separate `localhost` cookies by port. Without it, two worktrees would keep overwriting each other's session, and each instance would reject the other's signature.
- `localhost:<web>` and `localhost:<api>` are same-site (same scheme and host; the port is ignored), so SameSite=Strict cookies are sent on the page's `fetch` calls.
- Stateless: no sessions table and no write when a session is issued, so the "writes are asynchronous" rule holds. The protected payload carries the session id. Expiry is enforced by the protector.
- Data Protection keys are persisted with `PersistKeysToDbContext<CartDbContext>` (`ushopDataProtectionKeys` table, application name `uShopDemo.Api`), so sessions survive an API restart. Each instance has its own database and therefore its own keys.
- Middleware (after `UseRouting` and `UseCors`, before endpoint execution and body binding) resolves the session for every cart, operation and order endpoint, marked by endpoint metadata, into a scoped `SessionContext` and returns an empty 401 without one, so a missing session is reported before any 415 or 400. CORS preflights, `/session` and `/health` are not covered. It never writes the session id to logs or responses.

Alternative: bearer token kept in `localStorage` and sent in a header. Simpler CORS, but any script on the page can read the token (the user chose the cookie). Alternative: a sessions table. Revocable, but every new visitor would need a synchronous write.

**Ownership.** `Cart.SessionId` (Guid, required, indexed).
- At acceptance, write endpoints read the cart. If it exists and belongs to another session, they return 404 and queue nothing. Cart reads filter by `SessionId`, so a foreign cart reads as empty, the same as an unknown id. Writes return 404 because an accepted write would otherwise show up as a confusing failure later. This tells the caller that the id is in use, which is acceptable for random GUIDs.
- Each queued command carries its `SessionId`. The worker re-checks ownership. When it creates the cart, it inserts with the command's session; if that insert hits the primary key (another session created the cart first), the operation fails. This settles the race.
- `OperationTracker` entries store their owning session. `GET /operations/{id}` and the pending/rejected branch of `GET /orders/{n}` return 404 to other sessions. Confirmed orders are filtered by `Cart.SessionId`.

**Queue and worker.** A singleton bounded `Channel<CartCommand>` (capacity 1000, `FullMode = Wait`, single reader) and a `BackgroundService` that reads it. Each command is processed in a fresh DI scope with its own `DbContext`. One reader processes commands in acceptance order across all carts, which satisfies the per-cart ordering requirement with no locking. A failed command marks its operation `failed`, is logged, and the worker continues. Alternatives: per-cart partitioned channels (more throughput, unnecessary for a demo); RabbitMQ/Service Bus (rejected by the user: extra container per worktree).

**Operation tracking.** `OperationTracker` singleton over `IMemoryCache`: operation id → status, error, and for checkouts the order number. Entries expire 10 minutes after their last update. The tracker also maps order number → operation id so `GET /orders/{n}` can answer `pending`/`rejected` before (or instead of) a stored order. Order status lookup: stored order with status Ordered → `confirmed`; else tracked checkout → `pending` (queued, or completed but the stored order not yet visible to this read) or `rejected` (failed, with reason); else 404. Consequence: after an API restart, a never-processed order number returns 404. Acceptable given the queue is in-memory too.

**Order number at acceptance.** `US-yyMMdd-NNNNN` (random 5 digits) generated when checkout is accepted so it can be returned in the 202. Unique filtered index on `OrderNumber`; a collision during processing fails the operation (rejected).

**Data model.** `CartDbContext` in the API with `Cart` and `CartLine`, the same shape as today's entities plus `Cart.SessionId`, a unique index on `(CartId, ProductKey)`, and the Data Protection keys table (`IDataProtectionKeyContext`). Tables `ushopCart`, `ushopCartLine` and `ushopDataProtectionKeys`: the database is shared with Umbraco, so the prefix marks ownership. The API reads and writes only `ushop*` tables; the site never touches them. Migrations history in `__ushopMigrationsHistory` (`MigrationsHistoryTable`), separate from any EF history Umbraco keeps. The cart row is created by the worker on the first add. Writes to a cart whose status is Ordered fail with "cart already ordered"; reads of it return empty. Fresh `Initial` migration generated in the API project with `dotnet ef migrations add Initial --project src/uShopDemo.Api`. The site's migrations are deleted, not moved: every run starts on an empty database, so there is no data to carry over.

**Migrations at startup.** `Program.cs` applies `MigrateAsync()` in a scope after `Build()` and before `Run()`, so the API does not listen until the schema is current. Under Aspire, `WaitFor(umbracoDb)` guarantees the database exists first. Because `web.WaitFor(api)`, the API creates its tables before Umbraco's unattended install runs on the same database; task 4.2 verifies the install still succeeds. For SQLite (tests), `EnsureCreatedAsync()` instead, because the migrations are SQL Server-specific.

**Caller-supplied product data.** The add endpoint stores the name, SKU and price it is given. A comment on the endpoint and a README line state this is a demo simplification. The site renders these values into `data-*` attributes on the product form.

**CORS and CSRF.** Policy allowing exactly the origin in `USHOP_WEB_ORIGIN`, header `Content-Type`, methods GET/POST/PUT/DELETE, `AllowCredentials()`, exposed response header `Location`. The script uses `credentials: 'include'` and takes the operation id from the 202 body.

Because the session is a cookie, cross-site request forgery has to be addressed, without antiforgery tokens:
- SameSite=Strict: browsers don't send the cookie on requests started by another site.
- Writes with a body must be `Content-Type: application/json` (otherwise 415, after the session check: minimal-API body binding rejects non-JSON content types, and an endpoint filter also rejects `application/*+json` variants; the write endpoints carry "any content type" accepts metadata so routing does not reject the content type with its own 415 before the session middleware runs). A cross-origin JSON request always triggers a CORS preflight, and the preflight fails for any origin except the site's.
- DELETE always triggers a preflight.
- `POST /session` returns 403 when the request carries an `Origin` header other than `USHOP_WEB_ORIGIN` or the API's own origin (same-origin, used by the Scalar UI; not cross-site, so CSRF-neutral), so another site can't reset a visitor's session with a cross-site form post (the browser would otherwise accept the new cookie on the navigation response).
- No write endpoint uses GET.

**Health.** `/health` returns healthy only after migrations are applied (startup applies them before `Run()`) and the database check from the Aspire integration passes.

**API docs (added by Product Owner decision, 2026-11-02; ADR-0018).** Development only: `AddOpenApi()` + `MapOpenApi()` (`/openapi/v1.json`, OpenAPI 3.1) and Scalar `MapScalarApiReference()` (`/scalar`). Not session-protected. `/scalar` redirects to `/scalar/v1`. `/health` is added to the document by a document transformer, because health checks are not visible to ApiExplorer. Scalar can call `POST /session` (same origin, allowed by the `/session` origin rule under CORS and CSRF), so the other endpoints can be tried there. Every endpoint in the table above appears in the document; write endpoints document an `application/json` request body despite their "any content type" accepts metadata. Alternatives: Swashbuckle, SwaggerUI, Redoc, document only (see ADR-0018).

**AppHost wiring.**
```
var umbracoDb = sql.AddDatabase("umbracoDbDSN");
var api = builder.AddProject<Projects.uShopDemo_Api>("api")
    .WithReference(umbracoDb).WaitFor(umbracoDb)
    .WithHttpsEndpoint(port: apiPort, name: "https").WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "https");
var web = builder.AddProject<Projects.uShopDemo_Web>("web") /* as today */
    .WaitFor(api)
    .WithEnvironment("USHOP_API_URL", api.GetEndpoint("https"));
api.WithEnvironment("USHOP_WEB_ORIGIN", web.GetEndpoint("https"));
```
`WaitFor(api)` with `WithHttpHealthCheck` means the web process starts only once `/health` passes, so the first page load never sees a missing API. The services stay independent processes. If the API stops later, the site keeps running and the browser shows the "could not reach the cart service" message. The cross-reference (web → api URL, api → web origin) uses endpoint references only, so it does not create a startup dependency cycle. `apiPort` comes from `USHOP_API_PORT` (null = dynamic for plain `aspire run`; endpoint references then resolve to Aspire's proxied URLs, which are browser-reachable).

**Ports.** Add `api: 4` to `OFFSETS` in `scripts/lib/ports.mjs`. Block size stays 10. `dev.mjs` passes `USHOP_API_PORT` and prints the API URL; `worktrees.mjs list` prints it too. The block-availability probe covers the new port automatically.

**Site: client script and templates.** `wwwroot/js/cart.js` (ES module, no framework, no build step), loaded from `_Layout`. The API base URL reaches the page as `<meta name="ushop-api" content="...">`, rendered from `IConfiguration["USHOP_API_URL"]`. The script:
- Session: `POST /session` once per page load, before any other call; every request uses `credentials: 'include'`. The script never sees the session id.
- Cart id: `localStorage["ushop.cartId"]`, created with `crypto.randomUUID()` on first use, replaced with a new id when checkout is accepted. `localStorage` is per origin (including port), so worktrees don't share carts.
- Write helper: send request → on 202 poll `GET /operations/{id}` every 250 ms up to 15 s → resolve on `completed`, reject on `failed`/timeout. Buttons get `aria-busy` and are disabled while pending.
- Binding by data attributes, so views own markup: `[data-cart-count]` (header), `form[data-cart-add]` with `data-product-key/name/sku/price` (product page), `[data-cart]` with child `<template data-cart-line>`, `<template data-cart-empty>`, and `[data-field=...]` slots (cart page), `form[data-checkout]` with `[data-error-for=...]` (checkout), `[data-order]` with `<template data-order-pending|confirmed|rejected>` (confirmation, order number from `?order=`).
- Money formatting: `Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' })`, matching `Money.Format` (both give `£1,234.56`).
- Errors: a `[data-cart-status]` live region (role="status") per page receives messages such as "Could not reach the cart service."

Alternative: server-side site calls the API via `HttpClient` (rejected by the user: browser should call the API). Alternative: the script builds markup itself (rejected: layout branches would have to edit the script).

**Site: removals.** Delete `Shop/CartService.cs` (keep `Money` as `Shop/Money.cs`, formatting with `amount.ToString("C", CultureInfo.GetCultureInfo("en-GB"))`; explicit culture because the site language stays `en-US`), `Shop/CartSurfaceController.cs`, `Shop/Data/`, `Shop/ShopComposer.cs`, `Migrations/`; remove `Umbraco.Cms.Persistence.EFCore` and `Microsoft.EntityFrameworkCore.Design` package references; remove `@inject ICartService` from `_ViewImports`. The `.config/dotnet-tools.json` `dotnet-ef` entry stays (used for the API).

**Tests.** `tests/uShopDemo.Api.Tests` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`) hosting the API through `WebApplicationFactory` with the DbContext swapped for SQLite in-memory: a named shared-cache database per factory, held open by a keep-alive connection; each DbContext opens its own connection because `SqliteConnection` is not thread-safe alongside the hosted worker. Covers validation 400s, add→update ordering, add-twice merge, quantity 0 removal, checkout lifecycle (pending→confirmed, cart empty after), empty-cart rejection, unknown operation/order 404, CORS allowed/blocked origin, and the session rules: no cookie → 401, tampered cookie → 401, non-JSON write → 415, another session's cart/order/operation → 404, race for a new cart id → one failure. Each test client keeps its own cookie container (`HandleCookies`), so two clients act as two browsers. Alternative: Testcontainers SQL Server, more faithful but adds ~20 s per run and Docker in tests; the SQL Server path is exercised by the manual end-to-end verification.

## Risks / Trade-offs

- [Read-after-write lag: cart reads may not include a just-accepted write] → Client always waits for the operation to complete before re-reading.
- [Queued commands lost on API stop or crash] → Accepted for the demo; documented in README. Operation and pending-order status are also in-memory and lost.
- [Single worker is a throughput ceiling] → Irrelevant at demo scale; partitioning is the upgrade path.
- [Data Protection keys lost → every session invalid] → Keys are stored in `ushopDataProtectionKeys`. Losing the database also loses every cart, so nothing extra is lost.
- [Session cookies clash between worktrees on `localhost`] → The port is in the cookie name.
- [Visitor clears cookies → cart unreachable] → Accepted: the old cart id then reads as empty and writes to it return 404; the script replaces the cart id with a new one on a 404.
- [Stateless sessions can't be revoked before expiry] → Acceptable for anonymous carts. Rotating the Data Protection key ring revokes all sessions.
- [Site doesn't start while the API is unhealthy] → Intended. `aspire logs api` shows why.
- [Browser must trust the dev cert for a second port] → Same `localhost` dev cert as the site; nothing extra to trust.
- [Layout branches created before this change post to `CartSurfaceController`] → Branches 5.1-5.3 in `ushop-demo` are not created yet; build them after this change, or rebase and adopt the data-attribute hooks.
- [API tables exist before Umbraco installs into the same database] → Task 4.2 verifies the unattended install succeeds. If it does not, the fallback is `api.WaitFor(web)`, which changes the aspire-orchestration requirement; escalate to the Product Owner before switching.
- [Shared database couples site and API schema] → Disjoint, prefixed tables and a separate migrations history table; neither side references the other's tables.

## Migration Plan

No deployed environment and no persistent data: every instance starts on empty databases. Apply in one pass; rollback is reverting the branch. Archive `ushop-demo` before archiving this change (this change modifies its capabilities).
