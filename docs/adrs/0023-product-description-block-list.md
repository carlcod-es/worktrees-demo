# ADR-0023: Product description as a Block List with per-block views

- Status: Accepted
- Date: 2026-11-03
- Source: `openspec/changes/add-product-blocks/design.md`

## Context

Product `description` was HTML typed into a textarea and printed with `Html.Raw`. The Product Owner requires a Block List (`add-product-blocks` proposal) for a better editor experience. Layout branches restyle the site through `Views/**` and `wwwroot/**` only (ADR-0007), so block rendering must sit in views that each layout can override.

## Decision

- `product.description` becomes an `Umbraco.BlockList` with three element types: `richTextBlock`, `featureListBlock` and `quoteBlock`. There are no settings types, and inline and live editing are off. Rows show an icon and a label with a content snippet.
- Feature list items use `Umbraco.MultipleTextstring` (min 1).
- Rendering: `Views/Partials/blocklist/default.cshtml` (project copy) plus one `Views/Partials/blocklist/Components/<alias>.cshtml` per element type. The markup contract is in the design. Layouts override component views only.
- Category `intro` stays plain text on a dedicated `Category intro` TextArea (160 characters, mandatory). Categories get no blocks.

## Alternatives

- Block Grid: adds layout areas that editors don't need and makes product pages harder to keep consistent across layouts.
- Rich text editor alone, with no blocks: simpler, but gives no structured feature list or quote that layouts can style separately.
- Nested Block List for feature items: an overlay per item. Rejected for editor effort.

## Consequences

- Every block type needs a view in every layout that restyles it, and new block types need a view on `main` first.
- Seeded descriptions are Block List JSON in uSync content files, which is harder to hand-edit than HTML.
- Adding block settings later (for example a quote style) would need a new ADR.
