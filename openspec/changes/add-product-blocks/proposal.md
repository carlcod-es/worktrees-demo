## Why

Editors write a product's description as raw HTML in a plain textarea, and the product page prints it unchecked. Formatting is error-prone and a typo can break the page. No property in the content model tells editors where it appears or what to enter. This change gives editors structured blocks for product descriptions and adds guidance to every field. It lands on `main` before `ushop-demo` tasks 5.1-5.3 cut the layout branches, so each branch can restyle the blocks. Promotes `docs/feature-requests.md` FR-1.

## What Changes

- **BREAKING (content model)**: product `description` changes from an HTML textarea to a list of blocks (Product Owner constraint: Umbraco Block List). The database is ephemeral (ADR-0005), so nothing needs migrating.
- Three block types: rich text, feature list, quote. Blocks can be repeated and have no count limit. The description is optional, and an empty one renders nothing.
- Rich text is limited to bold, italic, links, lists and two heading levels. Tables, images, embeds, colours and fonts aren't allowed.
- In the editor, each block shows as a labelled row with an icon, its type and a content snippet. There's no visual preview.
- Each block type renders separately, so layouts can restyle one block type without touching the others.
- Seed data: all 12 descriptions become one rich-text block each, with the same text. 2-3 products also get a feature list and a quote.
- Category `intro` stays plain text. It becomes required and is capped at 160 characters, and markup typed into it is shown as literal text.
- Every page type and every property, including the block properties, gets a concise editor description saying where it is used and what to enter.
- FR-1's category body and image block are dropped. Category pages get no blocks, and product images stay in the gallery.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `ushop-site`: product detail renders the description from blocks; the seeded catalogue includes block content; new requirements for product description blocks, category intro and editor field guidance.

## Impact

- Content model and seed data under `src/uShopDemo.Web/uSync/v18`: product and category page types, guidance on all 7 page types, new block types and data types, all product content items.
- Views: `product`, plus a per-block rendering location for layouts to override. `wwwroot/css/site.css`.
- Docs: `docs/feature-requests.md` (FR-1 moved), and README if it describes content editing.
- Application code: one content-saving handler normalises heading levels (ADR-0025), and no new packages are added. Design: `design.md`, ADR-0023, ADR-0024, ADR-0025.
- `ushop-demo` tasks 5.1-5.3 wait until this change is merged.
