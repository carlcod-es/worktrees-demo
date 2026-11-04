# ADR-0021: Product gallery as server-rendered links enhanced by a hook-driven script

- Status: Accepted
- Date: 2026-11-03
- Source: `openspec/changes/add-shop-media/design.md`

## Context

The product page shows a main image and thumbnails (`ushop-site` requirement "Product gallery"). Choosing a thumbnail must swap the main image without a reload, by pointer and keyboard. Without script every image must still be reachable. Layout branches restyle the gallery but must not change shared scripts (ADR-0007, ADR-0013).

## Decision

- The view renders the full gallery: a main `<img>` and one `<a href="<image url>">` per thumbnail. Without script, a thumbnail link opens the full image.
- `src/uShopDemo.Web/wwwroot/js/gallery.js` (ES module, no framework, no build step, loaded from `_Layout`) intercepts thumbnail activation, swaps the main image's `src` and `alt`, and moves `aria-current="true"`. It finds elements by data attribute only and ignores pages without them.
- The data-attribute contract is in `docs/gallery-hooks.md`, on the same terms as `docs/cart-hooks.md`: layouts change markup and CSS around the hooks and don't edit the script.

## Alternatives

- CSS only (radio inputs or `:target`): no script, but radio-based galleries are awkward for screen readers and `:target` scrolls the page and fills history.
- Server round-trip (`?image=n`): works without script, but reloads the page, which the requirement forbids.
- Script builds the gallery markup: layouts would have to edit the script.

## Consequences

- Gallery behaviour is identical in every layout; only its look differs.
- Thumbnails are links, so keyboard users get native focus and Enter activation.
- `gallery.js` joins `cart.js` as a file that doesn't change between layouts.
