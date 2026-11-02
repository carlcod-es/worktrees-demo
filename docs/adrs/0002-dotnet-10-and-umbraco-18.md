# ADR-0002: .NET 10 and Umbraco 18 for the shop site

- Status: Accepted
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/proposal.md`, `design.md`

## Context

The talk needs a realistic, content-driven demo shop that installs and populates itself on a fresh worktree with no manual steps, and whose presentation can vary per branch.

## Decision

Build the site as an Umbraco 18 website (`uShopDemo.Web`) on .NET 10 (SDK pinned in `global.json`, roll-forward `latestFeature`). Umbraco installs unattended on first boot. Views read properties with `Value<T>()`; no generated models (`ModelsMode: Nothing`).

## Alternatives

None recorded in the source design.

## Consequences

- Umbraco 18 has no runtime Razor compiler without `Umbraco.Cms.DevelopmentMode.Backoffice`: views compile at build and view edits need a launcher restart.
- Content created through `IContentService` needs `TemplateId` set explicitly; template files are named by alias and rewritten by Umbraco on creation.
- The backoffice requires HTTPS, so the site is HTTPS-only (dev cert covers `localhost` on any port).
