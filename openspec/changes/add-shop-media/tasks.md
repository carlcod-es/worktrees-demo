Order: Section 1 → Section 2 → Section 3 → Section 4 → Section 5. Section 3 can start once 1.2 has passed review: it builds against the aliases fixed in `design.md`. Its verification needs Section 2. Paths, aliases and hooks: `design.md`. Requirements: `specs/ushop-site/spec.md`, `specs/layout-variants/spec.md`. ADR-0020, ADR-0021.

## 1. Artwork and manifest (worker-frontend; explicit assignment of `openspec/changes/add-shop-media/media-manifest.json`)

- [x] 1.1 Product artwork: 3 or 4 SVGs per product under `src/uShopDemo.Web/wwwroot/media/ushop/products/<sku-lower>-<n>.svg`, distinct views as listed in `design.md` "Artwork". At least 4 products have 3 images and at least 4 have 4. Verify: a Node one-liner lists 3-4 files per SKU for all 12 SKUs in `uSync/v18/Content`; every file parses as XML with an 800x600 viewBox, is under 3 KB, and contains no `<script`, ` on*=`, `<foreignObject` or external `href`.
- [x] 1.2 Category artwork `wwwroot/media/ushop/categories/<alias>.svg` (4 files), logo `wwwroot/media/ushop/logo.svg`, and a byte-identical copy `wwwroot/img/logo.svg`. Write `media-manifest.json` in the shape in `design.md` with one entry per file and an alt text for each. Verify: the manifest is valid JSON, every listed file exists, every file under `wwwroot/media/ushop/` is listed, every alt is non-empty, and the two logo files have equal SHA-256.

## 2. Content model and seeding (worker-backend; explicit assignment of `src/uShopDemo.Web/uSync/**` and `.gitignore`)

- [x] 2.1 `.gitignore` rules from `design.md`. Add `altText` to `umbracoMediaVectorGraphics`, add the `Product Images` and `SVG Image` data types, and make the `product` / `category` / `shop` property changes from `design.md` "Content model". Spike: write the `Products` folder plus one media item and point one product at it; delay the other content edits until this passes. Depends on 1.2. Verify: `git status` lists files under `wwwroot/media/ushop/` as untracked and not ignored. On a fresh `node scripts/dev.mjs`, the console shows a uSync import with no failed items, and that product's media file is reachable at `<site>/media/ushop/products/...` (200).
- [x] 2.2 All media items from `media-manifest.json` (folders `Products`, `Categories`, `Brand`) and every content reference: product `images` in manifest order, category `image`, shop `logo`. Remove all `image` values from products. Verify: `grep -rn "/img/products\|/img/categories" src/uShopDemo.Web/uSync` finds nothing. A fresh run's uSync import shows no failed items. A restart against the same database (stop and start the `ushop-web` resource in the dashboard) creates no duplicate media: the media count reported by uSync, or the item count in the media section's `Products` folder, is unchanged.

## 3. Views and gallery (worker-frontend; explicit assignment of `docs/gallery-hooks.md`)

- [x] 3.1 `docs/gallery-hooks.md` (hooks table from `design.md`, in the style of `docs/cart-hooks.md`) and `wwwroot/js/gallery.js` (ES module, no-op without `[data-gallery]`, loaded from `_Layout` with `asp-append-version`). Verify: `dotnet build uShopDemo.slnx` succeeds. The script only references hooks listed in the doc (`grep -o "data-gallery[a-z-]*"` on both, compared).
- [x] 3.2 `product.cshtml` gallery: main image plus thumbnail links per `design.md`. The first thumbnail has `aria-current="true"`, the main image's alt is the image's `altText`, and an empty gallery renders nothing. Verify: `dotnet build uShopDemo.slnx` succeeds. On a fresh run, `curl -sk <site>/<product-url>` contains one `data-gallery-main` and 3 or 4 `data-gallery-thumb` links whose `href` values return 200.
- [ ] 3.3 Product cards (`Partials/productCard.cshtml`) and home category cards (`shop.cshtml`) use the first or single media image with the decorative pattern. `_Layout` brand shows the logo (alt = `siteTitle`) linked home, with a text fallback. `wwwroot/errors/500.html` and `4xx.html` brand uses `/img/logo.svg`. CSS for gallery, thumbnails, current thumbnail and logo (header height ~40px, text sizes still at least 18px). Delete `wwwroot/img/products/` and `wwwroot/img/categories/`. Verify: `dotnet build uShopDemo.slnx` succeeds, `grep -rn "Value(\"image\")\|/img/products\|/img/categories" src/uShopDemo.Web/Views src/uShopDemo.Web/wwwroot` finds nothing, and every page is free of horizontal overflow at 1280x720.

## 4. Docs (Architect; explicit paths)

- [x] 4.1 `docs/feature-requests.md`: mark FR-2 and FR-3 as moved into `add-shop-media` (same form as FR-4), and note that image crops are dropped because SVG scales. `README.md`: one line on where seeded media lives and the ADR-0020 rule for changing it. Verify: both files read on one screen each, and FR-2 and FR-3 cite `add-shop-media`.

## 5. Verification (Product Owner, Firefox)

- [ ] 5.1 On a fresh `node scripts/dev.mjs`:
  - home shows category images and the header logo; the logo alt is the site title and it links home;
  - every category page shows each product's first image;
  - a 3-image and a 4-image product show the right thumbnail counts;
  - thumbnails swap the main image by click and by Tab + Enter with no reload, and the current thumbnail is marked;
  - with JavaScript disabled (`about:config` `javascript.enabled` = false), the main image shows and each thumbnail opens its image;
  - the backoffice media section lists all images with alt text;
  - uploading an SVG creates a media item whose URL loads;
  - reordering a product's images and publishing changes the page and card order;
  - removing the logo and publishing shows the text title;
  - `/errors/500.html` and `/errors/4xx.html` show the logo;
  - at 1280x720 (Responsive Design Mode) on home, a category, a 3-image and a 4-image product, cart, checkout, a 404 URL and both static error pages, the console check `[...document.querySelectorAll("body *")].filter(e => e.getBoundingClientRect().right > document.documentElement.clientWidth).length === 0` is `true`; if not, drop `.length === 0` to list the offending elements (`body` hides horizontal scrollbars, so look for overflow this way, not by scrollbar); this also closes `add-shop-media` task 3.3;
  - `git status` is clean afterwards apart from ignored uploads.
