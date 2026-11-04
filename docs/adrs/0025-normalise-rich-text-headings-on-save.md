# ADR-0025: Normalise rich-text heading levels on save

- Status: Accepted
- Date: 2026-11-04
- Source: Product Owner decision during `add-product-blocks` apply (task 1.1 spike finding); `openspec/changes/add-product-blocks/design.md`
- Supersedes: ADR-0024 (clause "There's no server-side or render-time HTML sanitiser" only, for heading levels)

## Context

ADR-0024 relies on the Tiptap schema to remove disallowed formatting. The `add-product-blocks` task 1.1 spike found that Umbraco 18 registers the Heading extension with levels 1-6 and offers no setting to restrict them. The toolbar offers only H2 and H3, but pasted or typed h1 and h4-h6 survive. The `ushop-site` requirement "Rich-text formatting" allows two heading levels below the page title.

## Decision

- A content-saving notification handler in `src/uShopDemo.Web/Shop/` rewrites heading levels in product `description` rich-text blocks (`richTextBlock.text`): h1 becomes h2, and h4, h5 and h6 become h3. Attributes and content are kept.
- It runs on every save, including the uSync first-boot import, so stored content, the editor and every layout agree.
- No new package. Other disallowed formatting is still removed by the editor schema (ADR-0024).

## Alternatives

- Rewrite in the rich-text block view at render time: no application code, but every layout override must keep it, and the editor still shows the wrong level.
- Custom backoffice Tiptap extension restricting levels to 2-3: fixes it at input, but adds a client-side extension pattern to the repo, for more work.
- Relax the spec: rejected by the Product Owner.

## Consequences

- Adds application code to the web project, so the `add-product-blocks` proposal's "no application code" no longer holds.
- An editor who pastes an h1 sees it as h2 after saving.
- New rich-text properties need adding to the handler if they should be normalised.
