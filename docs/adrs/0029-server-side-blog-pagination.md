# ADR-0029: Server-side blog pagination with a query string and a shared helper

- Status: Accepted
- Date: 2026-11-04
- Source: `openspec/changes/add-blog/design.md`

## Context

The listing shows 5 articles per page, and each page must work without script and have its own URL (`blog` spec). Layouts must be able to replace the listing view without changing which articles appear where (ADR-0007). `ModelsMode` is `Nothing`, and the only custom server code today is the heading handler.

## Decision

- Page URLs are `/blog/` and `/blog/?page=N`. Links to page 1 use `/blog/`.
- `src/uShopDemo.Web/Shop/Blog/BlogListing.cs` holds the rules as a static helper: published `blogArticle` children, sorted by `publishDate` descending then title, 5 per page, with the page value clamped. Views call it and render the result, so a layout can't change them by accident.
- The paging arithmetic is a pure function, separate from Umbraco types.
- An invalid or out-of-range value shows the nearest valid page with a 200 status.

## Alternatives

- Path URLs (`/blog/page/2/`): tidier, but need a custom content finder and more routing code for no behaviour gain.
- Client-side paging or "load more": fails without script and has no shareable page URLs.
- Paging in `blog.cshtml`: no C#, but every layout's copy could drift, breaking the `blog` requirement "Blog layout independence".
- 404 for out-of-range pages: more correct for crawlers, but needs a render controller, and a page can vanish when an article is unpublished.

## Consequences

- `?page=99` serves the same content as the last page. Acceptable for a demo; no canonical link is added.
- Sorting runs in memory per request, which is fine for this number of articles.
- The page size is a constant, so changing it needs a code change.
