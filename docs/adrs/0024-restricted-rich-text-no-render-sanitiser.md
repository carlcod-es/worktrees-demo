# ADR-0024: Restricted rich-text editor; no render-time sanitiser

- Status: Accepted; heading-level clause superseded by ADR-0025
- Date: 2026-11-03
- Source: `openspec/changes/add-product-blocks/design.md`

## Context

The `ushop-site` requirement "Rich-text formatting" (`add-product-blocks`) allows only bold, italic, links, lists and two heading levels, and disallowed formatting must not render, even when pasted. The existing `Richtext editor` data type enables tables, images, embeds, inline styles and the source editor.

## Decision

- New data type `Product rich text` (`Umbraco.RichText`, Tiptap). Extensions: `RichTextEssentials`, `Bold`, `Italic`, `Heading`, `BulletList`, `OrderedList`, `Link`, `TrailingNode`. Toolbar: Bold, Italic, Heading 2, Heading 3, Bullet list, Ordered list, Link, Unlink. No source editor, media, embed, table, style, class, block or text-align extensions.
- Disallowed content is removed by the editor's schema on input and paste. There's no server-side or render-time HTML sanitiser.
- The existing `Richtext editor` data type stays as it is and isn't used by the shop.

## Alternatives

- Render-time sanitiser (for example an HTML sanitiser package): defends against content that bypasses the editor, such as hand-edited uSync files or API writes. It needs a new dependency, and the only authors here are trusted editors and the repo itself.
- Reuse `Richtext editor` with a trimmed toolbar: extensions stay loaded, so pasted tables and styles survive.

## Consequences

- Content written outside the editor (uSync files, the Management API) isn't filtered, so reviewers check seeded markup.
- If the Heading extension can't be limited to levels 2-3, pasted h1 or h4 may survive. That gets escalated, and a sanitiser would need a new ADR.
