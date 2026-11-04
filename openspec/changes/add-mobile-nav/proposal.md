## Why

On narrow screens the header's category links wrap onto extra lines and push the page down. No requirement covers navigation below laptop width. Product Owner request (reported as a bug): collapse the category links into a menu button on mobile.

## What Changes

- Below 768px (48rem, the site's existing breakpoint), the category links collapse behind a "Menu" button. The logo and cart link stay visible.
- The button opens and closes the menu by pointer or keyboard, reports its expanded state to assistive technology, and Escape closes the menu.
- At 768px and wider, nothing changes: links show inline, with no button.
- Without script, the links show (stacked under the header on narrow screens) and there is no button.
- The toggle is a small shared client script driven by data attributes, like the gallery (ADR-0026). Layout branches restyle around the hooks and don't edit the script.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `ushop-site`: new requirement "Mobile navigation"; "Presentation confined to views and static assets" adds the navigation script to the files that don't change between layouts.

## Impact

- Views: `Views/Shared/_Layout.cshtml` (toggle button, hooks, script tag). `wwwroot/css/site.css`. New `wwwroot/js/nav.js`.
- Docs: new `docs/nav-hooks.md`, README layout tree and layout-change paragraph. ADR-0026.
- No new packages, no content model or application code changes.
- Lands on `main` before `ushop-demo` tasks 5.1-5.3 cut the layout branches.
