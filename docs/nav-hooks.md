# Navigation hooks

Contract between `src/uShopDemo.Web/wwwroot/js/nav.js` and the views (ADR-0026). `nav.js` finds elements by data attribute and does nothing in a `[data-nav]` container that lacks a toggle or menu. The header may change freely as long as these hooks stay.

## Rules

- Change markup and CSS around the hooks.
- Don't edit `nav.js`.
- Render the toggle with `hidden`, so it stays out of the way without script. The script removes `hidden`.
- The toggle's `aria-controls` must match the menu's `id`.
- CSS decides visibility by breakpoint; the script never reads the viewport. Below the breakpoint, hide the menu only under `[data-nav-ready]:not([data-nav-open])`. At and above it, hide the toggle and always show the menu, so a resize never leaves the menu stuck closed.
- Keep the toggle's accessible name "Menu": visible text, or an icon plus visually hidden text.
- Keep `[hidden] { display: none !important; }` (in `site.css`), or set `display` on the toggle only under `:not([hidden])`. Otherwise the toggle shows without script and does nothing.
- Keep the current-item marker (`.active` in `site.css` or similar) visible in the open menu.

## Hooks

| Hook | Script behaviour |
|---|---|
| `[data-nav]` | Container (the header). Scopes the hooks below. Gets `data-nav-ready` on init and `data-nav-open` while the menu is open. |
| `button[data-nav-toggle]` | `type="button"`, `hidden`, `aria-expanded="false"`, `aria-controls="<menu id>"`. Click (or Enter/Space, native to a button) toggles `aria-expanded` and `data-nav-open`. |
| `[data-nav-menu]` | The element shown and hidden; needs an `id`. |

Escape anywhere on the page while the menu is open closes it and moves focus to the toggle. The listener is on `document`, because Firefox and Safari on macOS don't focus a button when it is tapped or clicked.

## Example

```html
<header data-nav>
  <a href="/">Shop</a>
  <button type="button" data-nav-toggle hidden aria-expanded="false" aria-controls="site-nav"><span aria-hidden="true">☰</span> Menu</button>
  <nav id="site-nav" data-nav-menu aria-label="Main">
    <a href="/coffee/" class="active">Coffee</a>
    <a href="/grinders/">Grinders</a>
  </nav>
</header>
```

```css
@media (width < 48rem) {
  [data-nav-ready]:not([data-nav-open]) [data-nav-menu] { display: none; }
}
@media (width >= 48rem) {
  [data-nav-toggle] { display: none !important; }
}
```
