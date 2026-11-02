# ADR-0015: Friendly error pages in every environment

- Status: Accepted
- Date: 2026-11-02
- Source: `openspec/changes/ushop-demo/design.md`

## Context

The site showed the default Umbraco 404 and, under the launcher's Development environment, the developer exception page for errors. A tampered add-to-cart post returned 500. The Product Owner asked for graceful 4xx and 5xx pages, shown in Development too, because the demo runs in Development on stage.

## Decision

- 404: Umbraco `notFound` document type, template (`Views/notFound.cshtml`, rendered through `_Layout`) and content node in uSync, configured with `Umbraco:CMS:Content:Error404Collection`.
- 500: `UseExceptionHandler` with a handler that sets 500 and sends `wwwroot/errors/500.html`, before `UseUmbraco()`, in every environment. Styled with `site.css`.
- Other 4xx: `UseStatusCodePages` with a handler that sends `wwwroot/errors/4xx.html` and keeps the original status, for responses without a body.
- Both handlers apply to site paths only; `/umbraco/*` keeps Umbraco's own handling (including the developer exception page in Development).
- `wwwroot/errors/500.html`, `wwwroot/errors/4xx.html` and the `notFound` template/view are fixed paths; layout branches restyle them but must not rename or move them.
- Invalid product key on add-to-cart returns 400.

## Alternatives

- Razor or Umbraco-rendered 500 page: shows nav and badge, but can fail on the same fault it reports (for example the database being down).
- Static 404 page: simpler, but loses nav and branch badge, and layout branches must restyle it separately.
- Developer exception page in Development: standard, but the audience would see stack traces.
- Re-executing the request to the static page path (`UseExceptionHandler("/errors/500.html")`, `UseStatusCodePagesWithReExecute`): the static file middleware resets the status to 200.

## Consequences

- For site paths, exception details appear only in logs and the Aspire dashboard, including locally.
- Static error pages carry the demo notice but no branch badge.
- The 404 page is content: layout branches restyle it through the view and CSS, not by editing content.
