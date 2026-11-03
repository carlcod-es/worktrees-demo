# ADR-0020: Images as SVG media, seeded through uSync with committed files

- Status: Accepted
- Date: 2026-11-03
- Source: `openspec/changes/add-shop-media/design.md`; Product Owner constraint (SVG media in Umbraco)
- Supersedes: ADR-0006 (images clause only: "Images are static files in `wwwroot/img` referenced by path, not Umbraco media")

## Context

Product galleries, category images and a site logo (`docs/feature-requests.md` FR-2, FR-3) must be editable in the backoffice and stored as SVG. Every run starts on an empty database (ADR-0005), so media items and their files must be recreated on first boot with no manual steps. `wwwroot/media/` is git-ignored because it holds runtime uploads.

## Decision

- Images are Umbraco media of the built-in `umbracoMediaVectorGraphics` (Vector Graphics (SVG)) type, extended with a mandatory `altText` property.
- Media items are uSync files in `src/uShopDemo.Web/uSync/v18/Media`, imported by the existing first-boot import (ADR-0006). Their `umbracoFile` values point at files committed under `src/uShopDemo.Web/wwwroot/media/ushop/`.
- `.gitignore` ignores `wwwroot/media/*` except `wwwroot/media/ushop/`, so seeded files are tracked and runtime uploads (`wwwroot/media/<random>/`) stay ignored.
- Content references media by key through media pickers: product `images` (multiple), category `image` (single), shop `logo` (single).
- No image crops: SVG scales without them.

## Alternatives

- uSync.Complete media file sync: ships files with exports, but adds a paid package and dependency.
- Startup code that creates media from files through `IMediaService`: returns to code seeding, which ADR-0006 replaced; content changes would need C# edits.
- Keep static files referenced by path: fails the constraint (not backoffice-managed, no per-image alt text).

## Consequences

- Seeded media files are content. Layout branches must not change `wwwroot/media/` (`layout-variants` spec).
- Changing seeded media follows ADR-0017: edit on `main`, export, commit `uSync/v18` and the matching files under `wwwroot/media/ushop/`. An upload in the backoffice lands in an ignored folder, so it has to be moved under `wwwroot/media/ushop/` (with its `umbracoFile` path updated) before commit.
- The static error pages can't read media, so they use a static copy of the logo (`wwwroot/img/logo.svg`) that must be kept identical by hand.
