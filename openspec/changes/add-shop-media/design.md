## Context

See `proposal.md` for motivation. Current state (observed):
- Products and categories each have an `image` text property holding a path (`/img/products/acc-kettle.svg`). The files are 16 flat SVGs (800x600) under `wwwroot/img/`.
- Views read images with `Value("image")`: `product.cshtml`, `shop.cshtml` (category cards, `alt=""`), `Partials/productCard.cshtml` (decorative, `aria-hidden`). The header brand is a text link to `siteTitle`. Static error pages hard-code a text brand `uShop`.
- uSync 18 already exports the built-in `umbracoMediaVectorGraphics` media type (`uSync/v18/MediaTypes`). No `Media/` folder exists yet. `FirstBootGroup: All` includes media.
- Umbraco 18.3's default `DisallowedUploadedFileExtensions` (`ashx,aspx,ascx,config,cshtml,vbhtml,asmx,air,axd,xamlx`) doesn't block `.svg`.
- `.gitignore` ignores `src/uShopDemo.Web/wwwroot/media/`.
- Views run with `ModelsMode: Nothing` and read properties with `Value<T>()`.

## Goals / Non-Goals

**Goals:**
- Seeded media and its files are recreated on every fresh boot with no manual steps.
- The gallery works with pointer, keyboard and no script, in every layout, without layout branches editing shared code.

**Non-Goals:**
- Raster images, image crops, responsive `srcset`.
- A lightbox or zoom.
- A committed generator script for media or uSync files.
- Sanitising uploaded SVGs.

## Decisions

ADRs: ADR-0020 (media storage and seeding), ADR-0021 (gallery script). Superseded: images clause of ADR-0006.

**File layout.** Seeded files live under `src/uShopDemo.Web/wwwroot/media/ushop/`:
- `products/<sku-lower>-<n>.svg`, with `n` = 1..4 in gallery order. The existing image is redrawn or reused as `-1`.
- `categories/<category-alias>.svg`, with aliases `coffee`, `brewers`, `grinders`, `accessories`.
- `logo.svg`.
- Static copy for error pages: `wwwroot/img/logo.svg`, byte-identical to `media/ushop/logo.svg`.
- `wwwroot/img/products/` and `wwwroot/img/categories/` are deleted once nothing references them.

`.gitignore`:
```
src/uShopDemo.Web/wwwroot/media/*
!src/uShopDemo.Web/wwwroot/media/ushop/
```

**Artwork.** Hand-authored flat SVGs in the style of the existing files: 800x600 viewBox, palette per category, no script, no event-handler attributes, no external references, no `<foreignObject>`. Products get 3 or 4 images; at least 4 products have 3 and at least 4 have 4, so layouts meet both counts. Views per product are chosen from: front, angle or side, detail or close-up, in use or with packaging. The logo is a wordmark "uShop" with a small mark, legible at 40px header height and on dark and light backgrounds.

**Media manifest (contract between artwork and seeding).** `openspec/changes/add-shop-media/media-manifest.json` (planning artifact, deleted at archive):
```json
{
  "logo":       { "file": "/media/ushop/logo.svg", "alt": "uShop" },
  "categories": [ { "alias": "coffee", "file": "/media/ushop/categories/coffee.svg", "alt": "..." } ],
  "products":   [ { "sku": "ACC-KETTLE", "images": [ { "file": "/media/ushop/products/acc-kettle-1.svg", "alt": "Gooseneck kettle, front view" } ] } ]
}
```
Alt text describes the image, not the product's merits. Pattern: `<product name>, <view>`. Product order and image order in the manifest are the gallery order.

**Content model (uSync).**
- `umbracoMediaVectorGraphics`: add `altText` (`Umbraco.TextBox`, mandatory, label "Alt text") on its existing tab.
- Data types (new):
  - `Product Images`: `Umbraco.MediaPicker3`, multiple, allowed type Vector Graphics, min 1, no max, crops off, focal point off.
  - `SVG Image`: same, single.
- `product`: remove `image`; add `images` (`Product Images`, mandatory).
- `category`: change `image` to `SVG Image` (alias unchanged).
- `shop`: add `logo` (`SVG Image`, optional).
- The database is ephemeral (ADR-0005), so the property type changes need no migration.

**Media items (uSync `Media/`).**
- Folders `Products`, `Categories`, `Brand` at the media root. One item per manifest entry, with fixed keys.
- `umbracoFile` = manifest path, `altText` = manifest alt, `umbracoExtension` = `svg`, `umbracoBytes` = file size.
- Content configs reference media keys in the MediaPicker3 value format. Hand-authored. Task 2.1 proves the format on one item before the rest are written.
- If a hand-authored file doesn't import, fall back: the Product Owner uploads that one item in the backoffice and exports it (ADR-0017), and the export becomes the template.

**Reading media in views.**
- `Model.Value<IEnumerable<IPublishedContent>>("images")` (MediaWithCrops is an `IPublishedContent`). Single pickers: `Value<IPublishedContent>("image")`.
- URL via `.Url()`, alt via `.Value<string>("altText")`.
- An empty or missing gallery renders no gallery and no broken `<img>`.

**Gallery markup and script (ADR-0021).** Hooks, documented in `docs/gallery-hooks.md`:

| Hook | Behaviour |
|---|---|
| `[data-gallery]` | Container; one per page section. |
| `img[data-gallery-main]` | Main image; `src`/`alt` replaced on swap. |
| `a[data-gallery-thumb]` with `href` = full image URL, `data-alt` = alt text | Click/Enter: `preventDefault`, swap main, set `aria-current="true"` on it and remove from siblings. Accessible name from its inner `<img alt>` (`<alt text>`). |

- Server renders the first thumbnail with `aria-current="true"`.
- Focus stays on the activated thumbnail.
- `gallery.js` is an ES module loaded from `_Layout` with `asp-append-version`, and is a no-op without `[data-gallery]`.
- A layout may hide thumbnails when there is one image, but must keep the hooks.
- Product cards keep the decorative pattern (`alt=""`, `aria-hidden` link) using the first image. Category cards on the home page do the same, because the category name is the adjacent link text.

**Logo.**
- `_Layout`: if `logo` is set, `<a class="brand" href="<home>"><img src="<logo url>" alt="<siteTitle>"></a>`; otherwise the current text link.
- Error pages: `<a class="brand" href="/"><img src="/img/logo.svg" alt="uShop"></a>`. `uShop` is the seeded `siteTitle`; static pages can't read it.

**Ownership.**
- Artwork, the manifest, views, CSS, `gallery.js` and `docs/gallery-hooks.md`: worker-frontend. Explicit assignment for the manifest and the docs file.
- uSync and `.gitignore`: worker-backend, by explicit assignment.
- No application code changes.

## Risks / Trade-offs

- [Hand-authored uSync media and picker values don't match uSync 18's format] → Task 2.1 proves one item on a fresh boot first; fallback is a single backoffice upload and export (see Decisions).
- [SVGs uploaded through the backoffice are served from the site origin; one with script runs if opened directly] → Accepted for a localhost demo with one trusted admin. The site renders media only through `<img>`, where SVG script doesn't run. Seeded files are checked for `<script>`, `on*=` and `<foreignObject>`. Logged as a non-goal.
- [Static logo copy drifts from the media logo] → Byte-identical check in task 1.3; noted in ADR-0020.
- [About 50 SVGs add repo size and first-boot import time] → Simple flat shapes, target under 3 KB per file; cold start is already 1-2 min.
- [A backoffice upload during the demo lands in ignored `wwwroot/media/<random>/` and is lost on restart] → Intended for an ephemeral demo; the ADR-0020 consequence explains how to keep one.

## Migration Plan

None at runtime: every run starts on an empty database. Rollback is reverting the change's commit. Layout branches are created after this change is merged to `main` (`ushop-demo` Section 5).
