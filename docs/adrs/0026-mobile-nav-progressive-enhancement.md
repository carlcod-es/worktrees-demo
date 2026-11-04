# ADR-0026: Mobile navigation as a disclosure button enhanced by a hook-driven script

- Status: Accepted
- Date: 2026-11-04
- Source: `openspec/changes/add-mobile-nav/design.md`

## Context

Below 768px the header's category links must collapse behind a "Menu" button that reports its state to assistive technology and closes on Escape (`ushop-site` requirement "Mobile navigation"). Without script the links must still show. Layout branches restyle the header but must not change shared scripts (ADR-0007, ADR-0021).

## Decision

- `_Layout` renders the category links as today, plus a `<button hidden aria-expanded="false" aria-controls>` toggle.
- `src/uShopDemo.Web/wwwroot/js/nav.js` (ES module, no framework, no build step) unhides the toggle, toggles `aria-expanded` and an open-state attribute on the container, and closes on Escape. It finds elements by data attribute only.
- CSS decides visibility by breakpoint; the script doesn't read the viewport.
- The hook contract is in `docs/nav-hooks.md`, on the same terms as `docs/gallery-hooks.md`: layouts change markup and CSS around the hooks and don't edit the script.

## Alternatives

- `<details>/<summary>`, no script: a closed `<details>` hides its content at every width. Showing it on desktop needs `::details-content` (recent browsers only) or a duplicated nav, and Escape needs script anyway.
- CSS checkbox hack: no script, but the checkbox isn't announced as a menu button and has no expanded state.

## Consequences

- Navigation behaviour is identical in every layout; only its look differs.
- `nav.js` joins `cart.js` and `gallery.js` as files that don't change between layouts.
- Without script, narrow screens show the wrapped links.
