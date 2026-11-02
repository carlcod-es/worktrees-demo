## Why

Cart and checkout live inside the Umbraco site as form posts that reload the page and write to Umbraco's database. The demo needs a second, independently hosted service so a worktree runs a realistic multi-service stack, and cart/checkout should be asynchronous: the browser never waits on a database write.

## What Changes

- New `uShopDemo.Api` project: ASP.NET Core minimal API (.NET 10) for cart and checkout, run as its own Aspire resource on its own HTTPS port.
- Writes (add line, change quantity, remove line, checkout) are validated, put on an in-process queue (`System.Threading.Channels`) and answered with `202 Accepted` and an operation id. A background worker applies them to the database in order.
- Reads (cart, operation status, order) are async EF Core queries. Checkout returns an order number immediately; the order reads as `pending` until the worker confirms or rejects it.
- The API owns `ushop`-prefixed tables in the instance's single database (the one Umbraco uses), with its own EF Core migrations and migrations history table, applied at API startup. The app has one database per instance (ADR-0016).
- The browser calls the API directly with `fetch`; there are no page reloads for cart actions. The cart id is a browser-generated GUID kept in `localStorage`. The API allows credentialed CORS from the site's origin only.
- Each browser gets an API-issued session: a random 128-bit id signed with ASP.NET Data Protection, sent in an HttpOnly, Secure, SameSite=Strict cookie `__Host-ushop-session-<apiPort>`. Every cart, operation and order request requires it; a cart belongs to the session that first wrote to it and any other session gets 404. Knowing a cart id or order number is not enough to read or change it. Data Protection keys persist to the API's `ushopDataProtectionKeys` table, so sessions survive an API restart.
- Product name, SKU and unit price are sent by the browser and stored as given. Demo only: noted in code and docs that a real shop would look them up server-side.
- **BREAKING** (within this repo): `CartService`, `CartSurfaceController`, `ShopDbContext`, the site's EF Core migrations, the `Umbraco.Cms.Persistence.EFCore` reference and the site's shop migration step are removed. Existing carts in Umbraco's database are not migrated (every run starts on an empty database anyway).
- Cart, checkout, confirmation views and the header cart count render from API data through a shared script (`wwwroot/js/cart.js`) that fills `<template>` markup defined in the views, so layout branches keep control of markup and CSS.
- Prices display in pounds sterling, English format (`£1,234.56`), on both server-rendered and script-rendered pages. Only the symbol changes; stored catalogue prices are unchanged. The Umbraco site language stays `en-US`.
- AppHost: the site waits for the API to report healthy before it starts.
- Launcher: the port block gains an API port; `dev.mjs` and `worktrees.mjs list` print the API URL.

## Capabilities

### New Capabilities
- `cart-api`: independently hosted cart and checkout HTTP API: endpoints, queued asynchronous writes, operation and order status, per-browser sessions and ownership, its own tables and migrations in the shared instance database, CORS.

### Modified Capabilities
- `ushop-site`: product add-to-cart, cart and checkout use the cart API from the browser instead of form posts; persistence and migrations move out of the site; the cart is reachable only from the browser session that created it.
- `aspire-orchestration`: the AppHost adds the API resource and its database, wires the site and API URLs to each other, and starts the site only after the API is healthy; port isolation covers the API port.
- `worktree-launcher`: the launcher allocates and prints the API port.

Dependency: these modify capabilities introduced by the unarchived `ushop-demo` change. Archive `ushop-demo` before this one.

## Impact

- New project `src/uShopDemo.Api` added to `uShopDemo.slnx`; AppHost references it.
- `src/uShopDemo.Web`: shop C# code, `Migrations/`, EF Core packages removed; `Shop/Money.cs` formats GBP; `Views/_Layout`, `product`, `cart`, `checkout`, `orderConfirmation` and `_ViewImports` change; new `wwwroot/js/cart.js`.
- `src/uShopDemo.AppHost/AppHost.cs`: second database, API resource, cross-wired URLs.
- `scripts/lib/ports.mjs` (+ test), `scripts/dev.mjs`, `scripts/worktrees.mjs`, `README.md`.
- Queued writes are lost if the API process stops before the worker processes them; acceptable for a demo with ephemeral databases.
