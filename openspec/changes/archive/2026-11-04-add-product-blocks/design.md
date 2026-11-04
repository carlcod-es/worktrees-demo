## Context

- Umbraco 18.3, uSync 18.2, `ModelsMode: Nothing`, so views read properties by alias (ADR-0002). Content model and catalogue are uSync files imported on first boot. Changes happen on `main` by hand-editing `.config` files or by a backoffice export (ADR-0006, ADR-0017).
- Product `description` uses the shared `Textarea` data type (`c6bac0dd-…`), the same one as `summary` and category `intro`. `product.cshtml` prints it with `Html.Raw`.
- The existing `Richtext editor` data type enables most Tiptap extensions: tables, images, embeds, style, source editor and others. It stays unchanged and unused.
- Layout branches may change only `Views/**` and `wwwroot/**` (ADR-0007, `layout-variants` spec).
- No page type or property has a description today.

Requirements: `specs/ushop-site/spec.md`. Motivation: `proposal.md`. ADRs: ADR-0023, ADR-0024, ADR-0025.

## Goals / Non-Goals

**Goals:**
- Product description as a Block List with three element types, each rendered by its own view that a layout can override (ADR-0023).
- A dedicated rich-text configuration that allows only the formatting in the spec (ADR-0024).
- No new packages. The only application code is the heading normaliser (ADR-0025).

**Non-Goals:**
- Rendering-time HTML sanitising (ADR-0024). Heading levels are normalised on save instead (ADR-0025).
- Block settings (per-block style options), block grid, inline or live block editing.
- Changing the shared `Textarea`, `Textstring` or `Richtext editor` data types.

## Decisions

### Content model contract

`add-product-blocks` tasks sections 1 and 2 are built against these aliases.

| Element type | Alias | Icon | Properties (alias: editor, rule) |
|---|---|---|---|
| Rich text | `richTextBlock` | `icon-text-align-left` | `text`: Product rich text, mandatory |
| Feature list | `featureListBlock` | `icon-bulleted-list` | `heading`: Textstring, optional; `items`: Feature items, mandatory, at least 1 |
| Quote | `quoteBlock` | `icon-quote` | `quote`: Textarea, mandatory; `attribution`: Textstring, optional |

Element types live in an `Element types` folder in the backoffice. Their `IsElement` is true and they have no template.

New data types:

| Name | Editor | Config |
|---|---|---|
| Product description blocks | `Umbraco.BlockList` | the three element types; no min or max; inline editing off; live editing off; overlay size medium (`createModalSize`); no settings types |
| Product rich text | `Umbraco.RichText` (Tiptap) | ADR-0024 |
| Feature items | `Umbraco.MultipleTextstring` | min 1, max 0 (unlimited) |
| Category intro | `Umbraco.TextArea` | `maxChars` 160, 3 rows |

Property changes:
- `product.description` keeps its alias and key. It moves to `Product description blocks`, is renamed "Description" and stays optional.
- `category.intro` moves to `Category intro` and becomes mandatory, with a mandatory message.

Block labels in the editor use Umbraco Flavored Markdown with filters, so each row shows a snippet:

| Block | Label |
|---|---|
| Rich text | `Rich text: {umbValue: text \| stripHtml \| truncate:60}` |
| Feature list | Shows the heading, or the items when there is no heading (`Feature list: ${ truncate(heading || items.join(', '), 60) }`; fallback if it fails at runtime: `Feature list: {umbValue: heading} {umbValue: items}`; checked in `add-product-blocks` task 4.1) |
| Quote | `Quote: {umbValue: quote \| truncate:60}` |

The exact filter syntax gets checked in `add-product-blocks` task 1.1. If filters fail, drop them (`{umbValue: text}`). If no snippet can be shown at all, escalate, because the "Editor overview" scenario needs one.

Alternatives for `items`:
- Nested Block List of item elements. Heavier for editors, since each item opens an overlay.
- Textarea with one item per line. Gives no per-item reorder and makes "at least 1" fuzzy.

Repeatable textstrings give add, remove and drag-reorder per item at no cost.

### Rendering

- `product.cshtml` reads `Model.Value<BlockListModel>("description")`. It renders `<div class="description">` with `Html.GetBlockListHtmlAsync(...)` only when the list has items, so an empty description leaves no wrapper.
- `Views/Partials/blocklist/default.cshtml` (project-owned copy) loops over the items and renders `Views/Partials/blocklist/Components/<elementAlias>.cshtml`. A layout overrides any one of these files (ADR-0023).
- Markup contract, the base markup that layouts restyle:

| Block | Markup |
|---|---|
| Rich text | `<div class="block block-richtext">` + editor HTML (h2, h3, p, strong, em, a, ul, ol, li) |
| Feature list | `<section class="block block-features">` + optional `<h2>` + `<ul>` of `<li>` |
| Quote | `<figure class="block block-quote"><blockquote><p>` quote `</p></blockquote>` + optional `<figcaption>` attribution `</figcaption></figure>` |

  Plain-text values (`heading`, `items`, `quote`, `attribution`) are HTML-encoded by Razor. Quote line breaks are kept by CSS (`white-space: pre-line`), not converted to `<br>`.
- Category `intro` is already output with `@Model.Value("intro")`, which encodes it, so markup typed into it shows as literal text. No view change is needed. The "Markup in intro" scenario verifies it.

### Seeding

- Hand-author the uSync `.config` changes. Each product's `description` value becomes Block List JSON (`layout` / `contentData` / `settingsData` / `expose`) with fixed GUID keys, and the rich-text `text` value carries the existing HTML as `markup`.
- `add-product-blocks` task 1.1 is a spike on one product: a fresh boot imports it without failures, and a uSync export of that product round-trips to an equivalent value. The other products get converted only after the spike passes.
- Every product has rich text, then a feature list, then a quote. Brewing Scale, Ethiopia Yirgacheffe and Hand Grinder were the first three; the other 9 were added at the Product Owner's request (`add-product-blocks` task 1.5). Stoneware Mug's feature list has no heading, so the seed covers that label path. Copy is written to match each product, for example "What's in the box" for equipment and "Tasting notes" for coffee.

Alternative: author everything in the backoffice and export. That gives correct JSON, but it's slow and manual for 12 products, and the export adds noise (`CreateDate`, key order). Hand-authoring with a spike and a round-trip check is faster and repeatable.

### Heading normaliser

ADR-0025. `src/uShopDemo.Web/Shop/` holds a `ContentSavingNotification` handler, registered through a composer (`Program.cs` already calls `AddComposers()`). For `product` content it parses the `description` Block List JSON. For each `richTextBlock`, it rewrites the `text` value's `markup` so that `<h1` becomes `<h2` and `<h4`-`<h6` become `<h3`, with matching closing tags. Attributes are kept and nothing else changes. Keep the rewrite a pure static function so it can be checked in isolation. The `text` value reaches the handler as a JSON object from uSync and as a JSON string from backoffice or API saves, so the handler handles both. It re-serialises only when a heading changed. Feature items stay newline-joined strings (spike finding).

### Editor guidance text

Final wording for every description. Where text appears, it uses the visitor-facing names.

| Item | Description |
|---|---|
| Shop (type) | Site root. Holds site-wide settings; categories go under it. |
| Shop `siteTitle` | Site name. Shown on the home page, in the browser tab and footer, and as the logo's alt text. |
| Shop `tagline` | Short line shown under the site name on the home page and in the footer. |
| Shop `logo` | SVG logo for the page header. If empty, the site title is shown as text. |
| Category (type) | A product group. Lists its products and appears as a card on the home page. |
| Category `title` | Category name. Shown as the page heading and on its home-page card. |
| Category `intro` | Plain text, up to 160 characters. Shown under the heading and as the home-page card teaser. |
| Category `image` | Shown on the category's home-page card. |
| Product (type) | A product for sale. Has a detail page and a card in its category. |
| Product `title` | Product name. Shown as the page heading, on cards and in the cart. |
| Product `summary` | One or two sentences. Shown on product cards and under the price. |
| Product `description` | Optional. Blocks shown below the add-to-cart button, in this order. |
| Product `sku` | Stock code, e.g. `ACC-SCALE`. Shown on the product page and in orders. |
| Product `price` | Price in pounds, e.g. `24.50`. Shown everywhere the product is sold. |
| Product `images` | 3-4 images. The first is the main image and the card image; drag to reorder. |
| Cart, Checkout, Order confirmation, Not found (types) | `<Purpose>` page; one per site, under the shop root. |
| Their `title` | Page heading. |
| Rich text (type) / `text` | Paragraphs with headings, lists and links. / The text. Bold, italic, links, lists and two heading levels. |
| Feature list (type) / `heading` / `items` | A short list such as specs or what's in the box. / Optional heading above the list. / One short point per row; drag to reorder. |
| Quote (type) / `quote` / `attribution` | A quote from a customer or the roaster. / The quoted words, without quote marks. / Optional. Who said it, e.g. "Sam, Leeds". |

Implemented wording differs for category `title`, product `sku`, the `title` of cart, checkout, order confirmation and not found, and those four page types, to match the views (`add-product-blocks` task 1.3 report). The uSync files are authoritative. Workers check each line against the views. Where the text doesn't match what a view does, for example the summary position or the logo alt, they fix the text, not the view, and report the change.

## Risks / Trade-offs

- [Hand-authored Block List JSON doesn't match what uSync 18 expects] → Task 1.1 spike with a fresh-boot import and an export round-trip before converting the rest.
- [Heading levels can't be limited in the editor; h1 and h4-h6 survive paste (confirmed in the `add-product-blocks` task 1.1 spike)] → A content-saving handler rewrites h1 to h2 and h4-h6 to h3 (ADR-0025, `add-product-blocks` task 1.4).
- [Pasted HTML keeps other disallowed elements such as tables, images or colour] → Resolved: the `add-product-blocks` task 1.1 check in Firefox showed the Tiptap schema drops tables (cells kept as text), images, colour, underline and strike on paste.
- [Required and length rules aren't enforced on save] → Finding (`add-product-blocks` task 1.2): through the Management API, publish rejects an empty or 161-character intro and a feature list with no items; a plain save accepts them. The backoffice Save path is checked in `add-product-blocks` task 4.1. If it isn't blocked there, take it to the Product Owner and either change the "Category intro" scenarios through OpenSpec or apply the regex fallback `^[\s\S]{1,160}$`.
- [Changing `description`'s editor on an existing alias leaves old values] → The database is ephemeral (ADR-0005), so every run starts fresh. A long-lived database would need a migration, which is out of scope.
- [Layout branches copy `default.cshtml` and drift] → Layouts override only the component views they restyle. `docs/content-blocks.md` says so.

## Migration Plan

Merge to `main` before `ushop-demo` tasks 5.1-5.3. Fresh databases pick it up on first boot. Rollback means reverting the commit.
