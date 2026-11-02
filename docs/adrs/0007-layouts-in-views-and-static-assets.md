# ADR-0007: Layout variants confined to views and static assets

- Status: Accepted
- Date: 2026-10-31 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/ushop-demo/design.md`

## Context

The demo compares three layouts on parallel branches. Their diffs must be presentation-only so they compare cleanly and never conflict on behaviour.

## Decision

Layouts live only in `src/uShopDemo.Web/Views/` and `src/uShopDemo.Web/wwwroot/`. Branches `layout/minimal`, `layout/catalog` and `layout/showcase` branch from `main` and change only those paths. Content model, application code and (after `add-cart-api`) the cart client script do not change between layouts. The branch badge reads `USHOP_BRANCH`; the demo notice in `_Layout.cshtml` stays in every layout.

## Alternatives

None recorded in the source design.

## Consequences

- Behaviour that layouts must render (such as cart markup) has to be exposed through view-owned markup, not generated in code: this drives ADR-0013.
- Views compile at build (ADR-0002), so layout edits need a launcher restart.
