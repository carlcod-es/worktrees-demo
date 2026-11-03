# Feature requests

Backlog for later. Not planned or scheduled. Promote an item to an OpenSpec change before building it.

Content model and catalogue live in uSync (`src/uShopDemo.Web/uSync/v18`). Change them in the backoffice on `main`, run a uSync export, then commit the exported files (ADR-0017). Layout branches change only `Views/` and `wwwroot/`.

## FR-1: Block List for page content

Replace the plain textarea `description` (product) and `intro` (category) properties with a Block List. Editors then compose content from blocks such as rich text, image, feature list and quote.

- Element types for each block, plus one Block List data type.
- Views render blocks through partials under `Views/Partials/blocklist/`, so layout branches can restyle each block.
- Move the existing seeded description HTML into rich-text blocks.

## FR-2: Multiple images per product: moved

Moved into the `add-shop-media` change (SVG media, 3-4 images per product, gallery on the product page, first image on cards). Image cropper presets are dropped: SVG scales without crops (ADR-0020). See its `ushop-site` spec, requirement "Product gallery".

## FR-3: Site logo: moved

Moved into the `add-shop-media` change (SVG logo media picker on the `shop` root, `siteTitle` alt text, text fallback, static copy on error pages). See its `ushop-site` spec, requirement "Site logo".

## FR-4: Prices in pounds sterling: moved

Moved into the `add-cart-api` change (GBP, English format, symbol only, site language stays `en-US`). See its `ushop-site` spec, requirement "Price display".
