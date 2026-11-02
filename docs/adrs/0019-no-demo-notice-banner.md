# ADR-0019: No demo notice banner

- Status: Accepted
- Date: 2026-11-02
- Source: Product Owner decision; `openspec/changes/ushop-demo` (`ushop-site` spec)
- Supersedes: ADR-0007 (demo notice clause only), ADR-0015 (demo notice on static error pages only)

## Context

Every page, including the static error pages, showed "Demo shop: no real orders or payments are processed." The site is only ever shown as a demo on stage; the banner adds noise to the layout comparison.

## Decision

- Remove the banner from `_Layout.cshtml`, `wwwroot/errors/500.html` and `wwwroot/errors/4xx.html`, and its `.demo-notice` CSS.
- Layout branches have no notice to keep. The rest of ADR-0007 and ADR-0015 stands.
- The README keeps its "Demo only" line.

## Alternatives

- Keep it: the Product Owner judged it redundant.
- Move it to the footer: still noise; not requested.

## Consequences

- Nothing on a page says it is a demo; the README, the dummy checkout and the branch badge make that clear in context.
