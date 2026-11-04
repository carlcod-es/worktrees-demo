## Context

`Views/Shared/_Layout.cshtml` renders `header.site-header > .header-row` with the brand link, `nav.site-nav` (category links, current one `.active`) and the cart link. `.header-row` and `.site-nav` use `flex-wrap`, so links wrap on narrow screens. `site.css` already uses a `max-width: 48rem` breakpoint. `cart.js` and `gallery.js` are hook-driven ES modules that layouts don't edit (ADR-0013, ADR-0021).

## Goals / Non-Goals

**Goals:** the `ushop-site` requirement "Mobile navigation"; a hook contract layouts can restyle around.

**Non-Goals:** closing on outside click or on focus leaving the menu; animation; any change at 768px and wider; focus trapping (the menu is a disclosure, not a modal).

## Decisions

### Disclosure button + hook-driven script (ADR-0026)

Product Owner choice over `<details>/<summary>`: a closed `<details>` hides its content at every width, and forcing it open on desktop needs `::details-content` (recent browsers only) or a duplicated nav. Escape handling would need script anyway.

### Hooks contract (for `docs/nav-hooks.md`)

| Hook | Behaviour |
|---|---|
| `[data-nav]` | Container (the header). Scopes the hooks. On init the script sets `data-nav-ready` on it. |
| `button[data-nav-toggle]` | Rendered with `hidden`, `aria-expanded="false"` and `aria-controls` pointing at the menu's `id`. The script removes `hidden`. Activation toggles `aria-expanded` and `data-nav-open` on the container. |
| `[data-nav-menu]` | The element shown and hidden (`nav.site-nav`). Needs an `id`. |

- Escape (keydown on `document`, so it works when a tap leaves focus on the body, as in Firefox and Safari on macOS) while open: close, focus the toggle (section 1 supervisor rework).
- The script doesn't read the viewport. CSS decides visibility:
  - below 48rem: `[data-nav-ready]:not([data-nav-open]) [data-nav-menu]` is hidden; open menu lays out the links one per line under the header row;
  - 48rem and wider: the toggle is hidden and the menu always shows, whatever `data-nav-open` says. So resizing never leaves the menu stuck closed.
- No script: `data-nav-ready` is never set, the toggle keeps `hidden`, the menu shows (stacked under the header row below 48rem).
- Button content: visible text "Menu" with a decorative `☰` (`aria-hidden="true"`), so the accessible name is "Menu".
- Loaded from `_Layout` as `<script type="module" src="/js/nav.js" asp-append-version="true">`, same as the others. No framework, no build step.

### Header layout below 48rem

Row 1: brand, then toggle and cart link pushed right. Open menu: full-width row beneath, links stacked, each a tap target of at least 44px height. `.active` styling kept.

## Risks / Trade-offs

- [Flash of the expanded menu before the module runs: on mobile the links stack as 44px rows, then collapse] → Modules are deferred; the gap is small on a local demo. Accepted; logged in `docs/follow-ups.md`.
- [A layout branch sets `display` on the toggle, or drops the global `[hidden] { display: none !important; }` rule] → The toggle would show without script and do nothing. `docs/nav-hooks.md` states the rule.
- [Layout branches restyle the header heavily] → They keep the three hooks and the `id`/`aria-controls` pair; `docs/nav-hooks.md` states this.
