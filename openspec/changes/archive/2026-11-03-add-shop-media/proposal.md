## Why

The three layout branches (`ushop-demo` Section 5) need more visual material to look clearly different side by side. Today each product has one static image and the header is plain text. Product galleries, managed category images and a logo give each layout more to arrange. They go on `main` first so every layout branch inherits them. Promotes `docs/feature-requests.md` FR-2 and FR-3.

## What Changes

- Product, category and logo images become Umbraco media of the SVG media type, seeded on first boot from uSync files plus committed SVG files. Static `wwwroot/img/products` and `wwwroot/img/categories` are removed.
- **BREAKING (content model)**: product property `image` (text path) is replaced by `images` (multiple media picker). Category `image` becomes a single media picker. Existing images become the first gallery image. The database is ephemeral (ADR-0005), so nothing needs migrating.
- Each product has 3-4 generated SVG illustrations: distinct views such as front, angle, detail, in use. The number varies by product.
- The SVG media type gets an optional `altText` property (ADR-0022). Every seeded image has its own alt text, editable in the backoffice; the product gallery falls back to `<product title>, image <n>` when it is empty.
- Product page: main image plus thumbnails. Picking a thumbnail swaps the main image without a reload, by mouse or keyboard. Without script, each thumbnail links to its full image.
- Product cards (home, category) show the product's first image.
- Shop root gets a `logo` media picker with a generated SVG wordmark. The header shows the logo linked home, with the site title as alt text, and falls back to the text title when no logo is set. Static error pages show a static copy of the logo.
- SVG uploads work in the backoffice.
- New client script `wwwroot/js/gallery.js` with a view-hook contract (`docs/gallery-hooks.md`), on the same terms as `cart.js`: layouts restyle around the hooks and don't edit the script.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `ushop-site`: seeded catalogue includes managed SVG media with alt text; product detail shows a gallery; category listing and home show managed images; new site logo requirement; static error pages carry the logo; gallery script joins the files that don't change between layouts.
- `layout-variants`: presentation-only diffs exclude seeded media files under `wwwroot/media/`, which are content.

## Impact

- `src/uShopDemo.Web/uSync/v18`: MediaTypes (SVG `altText`), DataTypes (pickers), ContentTypes (`product`, `category`, `shop`), new `Media/` items, Content (all products, categories, shop root).
- `src/uShopDemo.Web/wwwroot/media/ushop/**` (new, committed), `.gitignore` (track only `wwwroot/media/ushop/`).
- Views: `product`, `shop`, `category`, `Partials/productCard`, `Shared/_Layout`. `wwwroot/errors/*.html`, `wwwroot/css/site.css`, new `wwwroot/js/gallery.js`, `wwwroot/img/logo.svg`.
- Docs: `docs/gallery-hooks.md`, `docs/feature-requests.md`, ADR-0020, ADR-0021, ADR-0022. Supersedes the images clause of ADR-0006.
- No new packages. No application code, API or migration changes.
- `ushop-demo` Section 5 (layout branches) waits until this change is merged.
