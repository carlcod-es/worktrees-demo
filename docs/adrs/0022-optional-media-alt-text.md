# ADR-0022: Media alt text is optional

- Status: Accepted
- Date: 2026-11-03
- Source: Product Owner decision during `add-shop-media` apply; `openspec/changes/add-shop-media/design.md`
- Supersedes: ADR-0020 (clause "extended with a mandatory `altText` property" only)

## Context

ADR-0020 made `altText` on `umbracoMediaVectorGraphics` mandatory. The Section 2 review of `add-shop-media` found that a mandatory media property may block or break quick (drag-and-drop) uploads in the backoffice, because the item is created before any alt text is entered. The Product Owner decided alt text must not be mandatory.

## Decision

- `altText` stays on `umbracoMediaVectorGraphics` (`Umbraco.TextBox`, label "Alt text"), but is optional.
- Every seeded media item still carries alt text (`media-manifest.json`).
- Views don't rely on it being present: the product gallery falls back to `<product title>, image <n>`; product and category cards are decorative (`alt=""`); the header logo uses `siteTitle`.

## Alternatives

- Keep it mandatory: forces alt text on every upload, at the risk of failing drag-and-drop uploads. Rejected by the Product Owner.
- Mandatory with a default value: Umbraco has no per-property default for media; would need application code.

## Consequences

- Uploaded images may have no alt text; editors add it afterwards in the media item.
- The gallery fallback text is generic, so screen-reader users get less detail for such images.
- The rest of ADR-0020 stands.
