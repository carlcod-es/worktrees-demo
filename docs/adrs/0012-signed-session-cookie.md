# ADR-0012: Stateless signed session cookie with cart ownership

- Status: Accepted; key storage location superseded by ADR-0016
- Date: 2026-11-02 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/add-cart-api/design.md`

## Context

Knowing a cart id or order number must not be enough to read or change it. Visitors are anonymous. Issuing a session must not need a synchronous database write. Several worktrees share `localhost` in one browser.

## Decision

`POST /session` issues or renews a random 128-bit id protected with an ASP.NET Data Protection `ITimeLimitedDataProtector` (purpose `ushop.session.v1`, 30 days), in cookie `__Host-ushop-session-<apiPort>`: HttpOnly, Secure, SameSite=Strict, Path=/. Keys persist to `shopdb` (`PersistKeysToDbContext<CartDbContext>`, application name `uShopDemo.Api`). An endpoint filter requires the session on cart, operation and order endpoints (401 otherwise). A cart belongs to the session that first wrote it (`Cart.SessionId`); other sessions read it as empty and get 404 on writes, operations and orders. The worker re-checks ownership and settles creation races on the primary key. CSRF is handled without antiforgery tokens: SameSite=Strict, JSON-only bodies (415 otherwise) so cross-origin writes always preflight, a CORS policy allowing only `USHOP_WEB_ORIGIN` with credentials, and no write over GET.

## Alternatives

- Bearer token in `localStorage` sent in a header: simpler CORS, but readable by any script on the page; the Product Owner chose the cookie.
- Sessions table: revocable, but every new visitor needs a synchronous write.

## Consequences

- The port in the cookie name stops worktrees overwriting each other's sessions on `localhost`.
- Sessions survive an API restart; losing `shopdb` loses keys and carts together.
- Sessions can't be revoked individually before expiry; rotating the key ring revokes all.
- Clearing cookies makes the old cart unreachable; the client replaces the cart id on a 404 write.
