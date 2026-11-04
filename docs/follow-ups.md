# Follow-ups

Non-blocking items logged during reviews, grouped by change. Promote an item to an OpenSpec change or task before fixing it.

## `add-cart-api`

- Remove the `System.Security.Cryptography.Pkcs` and `.Xml` version pins once upstream packages are patched.
- Tests:
  - `Origin: null` → 403;
  - null check on the Scalar redirect `Location`;
  - duplicate wait helper in `SessionTests`;
  - duplicate older 401 test;
  - `CorsTests` disposes a client the factory owns;
  - no test that server and client money formatting match.
- The card-number log scan covers `Cart` only.

## `add-shop-media`

- Gallery thumbnails load the full-size SVG (`Views/product.cshtml`); fine for small SVGs, revisit if raster images arrive.
- The current-thumbnail rule in `wwwroot/css/site.css` hard-codes `4px` in both the border and the `calc()`; changing one makes the thumbnail jump. Use a custom property.
- Product artwork bakes the product name and SKU into each SVG, so renaming a product leaves stale image text.
- `gallery.js` finds the main image with `gallery.querySelector`, which can match a nested gallery's main image; thumbnails already guard against nesting. Filter by `closest('[data-gallery]') === gallery`.
- Deleting a seeded media item (and emptying the recycle bin) or replacing its file in the backoffice deletes the tracked file under `wwwroot/media/ushop/`. Restore with `git checkout -- src/uShopDemo.Web/wwwroot/media/ushop`.

## `add-product-blocks`

- The rich-text block row label uses `stripHtml`, which joins paragraphs without a space ("ratios.USB-C"). Umbraco Flavored Markdown has no filter that keeps paragraph breaks.
- No automated tests cover `Shop/Content/HeadingNormaliser.cs` (h1 to h2, h4-h6 to h3, attributes and closing-tag whitespace, object and string values, malformed JSON, no change returns null). Needs a web test project, which is a shared `*.csproj` and needs explicit assignment.
- ADR-0025 says the normaliser runs during the uSync first-boot import. That's unverified, because no seeded markup has h1 or h4-h6.

## `add-mobile-nav`

- On mobile, the menu shows expanded (links stacked as 44px rows) until `nav.js` runs, then collapses. A small inline script in `<head>` that sets `data-nav-ready` early would remove the shift.
- `site.css` uses `max-width: 48rem` for the product and checkout single-column rule, but `width < 48rem` for the nav. At exactly 768px the nav is in desktop mode while those pages are single-column. Switch the older rule to `(width < 48rem)`.
- The global `[hidden] { display: none !important; }` in `site.css` sits under the `/* Cart script */` comment, but `nav.js` relies on it too (`docs/nav-hooks.md`). Reword the comment so layout branches keep it.

## `add-blog`

- No automated tests cover `Shop/Blog/BlogListing.cs` (`Clamp` edge cases, date order, the title tie-break). Seed dates are distinct, so the tie-break is checked by reading and by hand in `add-blog` task 6.1. Needs the same web test project as `HeadingNormaliser`.
- The seeded node can't be tested for duplicates on restart with `dev.mjs`, because every run gets an empty database (ADR-0005).
- `.claude/hooks/readonly-bash.mjs` doesn't block numbered redirects such as `2> file` or `&> file`: the pattern exempts them so that `2>&1` passes.
- Remove `.claude/hooks/readonly-bash.mjs` before the demo. Also remove its entry in `.claude/settings.json`, update the agents paragraph in `CLAUDE.md`, and drop the numbered-redirects item above.
