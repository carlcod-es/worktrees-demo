# Product description blocks

Product `description` is a Block List (ADR-0023). Editors add, reorder, repeat and remove blocks; an empty description renders nothing. Categories have no blocks: `intro` is plain text, required, at most 160 characters. Blog article `mainContent` uses the same block types and views through its own `Article blocks` data type (`docs/blog.md`), which requires at least one block.

## Block types

| Block | Alias | Properties | Rules |
|---|---|---|---|
| Rich text | `richTextBlock` | `text` | Required. Bold, italic, links, lists, H2 and H3 only (ADR-0024). Headings are normalised on save: h1 becomes h2 and h4-h6 become h3 (ADR-0025) |
| Feature list | `featureListBlock` | `heading`, `items` | Heading optional; at least one item, one per row |
| Quote | `quoteBlock` | `quote`, `attribution` | Quote required; attribution optional |

Required fields and the intro limit are enforced on publish.

## Markup contract

`product.cshtml` renders `<div class="description">` only when there are blocks. Inside it, `Views/Partials/blocklist/default.cshtml` renders `Views/Partials/blocklist/Components/<alias>.cshtml` for each block:

| Block | Markup |
|---|---|
| Rich text | `<div class="block block-richtext">` + editor HTML (`h2`, `h3`, `p`, `strong`, `em`, `a`, `ul`, `ol`, `li`) |
| Feature list | `<section class="block block-features">` + optional `<h2>` + `<ul><li>…` |
| Quote | `<figure class="block block-quote"><blockquote><p>…</p></blockquote>` + optional `<figcaption>` |

Plain-text values are HTML-encoded. Quote line breaks are kept by CSS (`white-space: pre-line`), not `<br>`.

## Restyling in a layout branch

- Change the component view(s) and `wwwroot/css/site.css`. Leave `default.cshtml` alone, so new block types keep rendering.
- Keep the property aliases. The content model (`uSync/**`) and `Shop/**` don't change between layouts (ADR-0007).

## Adding a block type (on `main` only)

1. In the backoffice, create an element type in the `Element types` folder, with a description on the type and every property.
2. Add it to both the `Product description blocks` and `Article blocks` data types, with an icon and a label (Umbraco Flavored Markdown, e.g. `Quote: {umbValue: quote | truncate:60}`).
3. Add `Views/Partials/blocklist/Components/<alias>.cshtml` and its CSS. Without that view, any product or article using the block fails with a 500 error.
4. Export from the uSync dashboard and commit `uSync/v18` (ADR-0017). If the block contains rich text that needs heading normalisation, extend `Shop/Content/HeadingNormaliser.cs`.
