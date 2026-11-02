# ADR-0006: Content model and catalogue in uSync files

- Status: Accepted; export mechanism superseded by ADR-0017
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/design.md`

## Context

Every run starts on an empty database (ADR-0005), so the content model, templates and catalogue must be recreated automatically. Editors should be able to change content in the backoffice and have the change land in git.

## Decision

Keep document types, templates and catalogue content as uSync 18 files in `src/uShopDemo.Web/uSync/v18`, imported on first boot (`uSync:Settings:ImportOnFirstBoot`). Backoffice edits re-export them (`ExportOnSave`). Images are static files in `wwwroot/img` referenced by path, not Umbraco media.

## Alternatives

- Code seeder through `IContentService`: used first, then replaced; content changes needed C# edits and could not round-trip from the backoffice.

## Consequences

- Content changes are made in the backoffice and committed as exported files.
- No media upload code; layout branches can restyle images freely.
- Moving images to Umbraco media (`docs/feature-requests.md` FR-2) would need media exports and shipped media files.
