# ADR-0009: Cart and checkout as form posts through an Umbraco SurfaceController

- Status: Superseded by ADR-0013
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/design.md`

## Context

Visitors add to cart, change quantities, remove lines and check out. Cart, checkout and confirmation pages need URLs and templates like every other page.

## Decision

`CartSurfaceController` handles POSTs for add, update quantity, remove and checkout, with Umbraco's form helpers for anti-forgery. `CartService` handles the cart-id cookie and EF access; views get the cart through `@inject ICartService`. Cart, checkout and confirmation are Umbraco content pages; confirmation reads the order number from TempData or the query string.

## Alternatives

None recorded in the source design.

## Consequences

- Every cart action reloads the page.
- Works without JavaScript.
- Cart behaviour is coupled to the site process and its views.
