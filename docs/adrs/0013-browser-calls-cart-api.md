# ADR-0013: Browser calls the cart API; views own the cart markup

- Status: Accepted
- Date: 2026-11-02 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/add-cart-api/design.md`
- Supersedes: ADR-0009

## Context

Cart actions should not reload the page. Layout branches must keep control of cart markup without touching shared code (ADR-0007).

## Decision

The browser calls the API directly with `fetch` and `credentials: 'include'`. One ES module, `src/uShopDemo.Web/wwwroot/js/cart.js` (no framework, no build step), loaded from `_Layout`, gets the API URL from a `<meta name="ushop-api">` tag rendered from `USHOP_API_URL`. It keeps the cart id in `localStorage`, polls operations after writes, and fills `<template>` elements and data-attribute slots defined in the views (`data-cart-count`, `data-cart-add`, `data-cart`, `data-checkout`, `data-order`, `data-cart-status`).

## Alternatives

- Site calls the API server-side via `HttpClient`: rejected by the Product Owner; the browser should call the API.
- Script builds markup itself: rejected; layout branches would have to edit the script.

## Consequences

- Cart and checkout require JavaScript.
- Layout branches change only views and CSS, including cart markup.
- Server-rendered and script-rendered values (such as prices) must be formatted identically on both sides.
- Layout branches created before this change must adopt the data-attribute hooks.
