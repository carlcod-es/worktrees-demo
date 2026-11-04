## 1. Navigation toggle (worker-frontend)

Contract: hooks table in `design.md` "Hooks contract"; ADR-0026.

- [x] 1.1 Add the toggle and hooks to `Views/Shared/_Layout.cshtml`, `wwwroot/js/nav.js`, and the header styles in `wwwroot/css/site.css`, per `design.md`. Verify on a fresh `node scripts/dev.mjs`, by fetching pages and reading the served files (no browser):
  - every Umbraco-rendered page (home, a category, a product, cart, checkout, 404) has `[data-nav]`, `button[data-nav-toggle][hidden][aria-expanded="false"]` whose `aria-controls` matches the `id` of `[data-nav-menu]`, and loads `/js/nav.js`;
  - `nav.js` only uses the three hooks and doesn't read the viewport;
  - the CSS hides the menu below 48rem only under `[data-nav-ready]` without `[data-nav-open]`, and hides the toggle at 48rem and wider;
  - `dotnet build uShopDemo.slnx` succeeds.

## 2. Docs (Architect; explicit paths)

Depends on 1.1.

- [x] 2.1 Add `docs/nav-hooks.md` (rules, hooks, example, as `docs/gallery-hooks.md`). In `README.md` add `wwwroot/js/nav.js` to the layout tree and name `nav.js` and `docs/nav-hooks.md` in the layout-change paragraph. At archive, add ADR-0026 to the `ushop-site` spec's ADR list. Verify: links resolve.

## 3. Verification (Product Owner, Firefox)

Depends on 1 and 2.

- [x] 3.1 On a fresh `node scripts/dev.mjs`, check each `ushop-site` "Mobile navigation" scenario: at 390x844 the menu is collapsed with logo, Menu and cart visible and no horizontal scroll; tap, Enter and Space open and close it; Escape closes it and focuses Menu; the current category is marked in the open menu; at 1280x720 links are inline with no button; with script disabled (`about:config` `javascript.enabled` false) links show and there's no button.
