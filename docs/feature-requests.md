# Feature requests

Backlog for later. Not planned or scheduled. Promote an item to an OpenSpec change before building it.

Content model and catalogue live in uSync (`src/uShopDemo.Web/uSync/v18`). Change them in the backoffice so uSync exports them, then commit the exported files. Layout branches change only `Views/` and `wwwroot/`.

## FR-1: Block List for page content

Replace the plain textarea `description` (product) and `intro` (category) properties with a Block List. Editors then compose content from blocks such as rich text, image, feature list and quote.

- Element types for each block, plus one Block List data type.
- Views render blocks through partials under `Views/Partials/blocklist/`, so layout branches can restyle each block.
- Move the existing seeded description HTML into rich-text blocks.

## FR-2: Multiple images per product

Each product currently has one `image` text property holding a path to an SVG in `wwwroot/img/products`. Replace it with a multiple media picker and add 3-4 images per product.

- Store the images as Umbraco media. uSync then needs media and content exports, and the media files must ship with the repo.
- Product page: main image plus thumbnails. Category cards: first image.
- Use image cropper presets for card and detail sizes.

## FR-3: Site logo

The header shows `siteTitle` as text (`Views/Shared/_Layout.cshtml`, `.brand`). Add a logo media picker on the `shop` root with an SVG logo.

- Render the logo with `siteTitle` as alt text, and fall back to the text title when no logo is set.
- Each layout branch may size and position the logo, but uses the same media item.

## FR-4: Prices in pounds sterling: moved

Moved into the `add-cart-api` change (GBP, English format, symbol only, site language stays `en-US`). See its `ushop-site` spec, requirement "Price display".
