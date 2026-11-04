# Follow-ups

Non-blocking items logged during reviews, grouped by change. Promote an item to an OpenSpec change or task before fixing it.

## `add-cart-api`

- Remove the `System.Security.Cryptography.Pkcs` and `.Xml` version pins once upstream packages are patched.
- Tests:
  - `Origin: null` → 403;
  - null check on the Scalar redirect `Location`;
  - the no-body check on error responses also passes on an empty body object;
  - duplicate wait helper in `SessionTests`;
  - duplicate older 401 test;
  - `CorsTests` disposes a client the factory owns;
  - no test that server and client money formatting match.
- The card-number log scan covers `Cart` only.
- A stale EF log filter in appsettings (kept on purpose).
- Launcher:
  - a comment in `shutdown.mjs` wraps unevenly;
  - the 500 ms duplicate-Ctrl+C window uses `Date.now` (`performance.now` would be steadier);
  - label padding in `dev.mjs` output.

## `add-shop-media`

- Gallery thumbnails load the full-size SVG (`Views/product.cshtml`); fine for small SVGs, revisit if raster images arrive.
- The current-thumbnail rule in `wwwroot/css/site.css` hard-codes `4px` in both the border and the `calc()`; changing one makes the thumbnail jump. Use a custom property.
- Product artwork bakes the product name and SKU into each SVG, so renaming a product leaves stale image text.
- `gallery.js` finds the main image with `gallery.querySelector`, which can match a nested gallery's main image; thumbnails already guard against nesting. Filter by `closest('[data-gallery]') === gallery`.
- Deleting a seeded media item (and emptying the recycle bin) or replacing its file in the backoffice deletes the tracked file under `wwwroot/media/ushop/`. Restore with `git checkout -- src/uShopDemo.Web/wwwroot/media/ushop`.
- The first uSync export (ADR-0017) of the hand-authored media and data type files will likely add `umbracoWidth`/`umbracoHeight`, real `CreateDate`s and reordered keys. That diff is noise, not drift.

## `add-product-blocks`

- The rich-text block row label uses `stripHtml`, which joins paragraphs without a space ("ratios.USB-C"). Umbraco Flavored Markdown has no filter that keeps paragraph breaks.
- No automated tests cover `Shop/Content/HeadingNormaliser.cs` (h1 to h2, h4-h6 to h3, attributes and closing-tag whitespace, object and string values, malformed JSON, no change returns null). Needs a web test project, which is a shared `*.csproj` and needs explicit assignment.
- ADR-0025 says the normaliser runs during the uSync first-boot import. That's unverified, because no seeded markup has h1 or h4-h6.
