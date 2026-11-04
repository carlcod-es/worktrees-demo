## Why

The site is shop pages only, with nothing for visitors to read. A humorous coffee blog gives the demo a second kind of content, editable in the backoffice and restylable per layout. Promotes `docs/feature-requests.md` FR-5. It should land on `main` before `ushop-demo` tasks 5.1-5.3 cut the layout branches, so each branch inherits the blog views.

## What Changes

- New blog listing page, linked from the main navigation after the categories.
- The listing shows all articles newest first, 5 per page, with a pagination control. The 10 seeded articles make 2 pages.
- New article pages, each with a title, publish date, summary, main image, 0-3 tags and main content built from the existing rich text, feature list and quote blocks.
- A single author, with no author page. Name, photo and bio are edited once, in an "About the author" tab on the blog listing page, and shown at the foot of every article page.
- Tags are display-only labels. They have no links, tag pages or filtering.
- Seed data: 10 humorous coffee articles, 10 SVG main images and 1 SVG author image, all imported on first boot.
- The heading rule for rich-text blocks (ADR-0025) extends to article content.
- The main navigation's accessible name changes from "Categories" to "Main", because it no longer lists only categories.

Out of scope: comments, search, RSS, an author page, tag pages, related articles, a home-page teaser, multiple authors and scheduled publishing.

## Capabilities

### New Capabilities
- `blog`: blog listing and pagination, article pages, author content, navigation link, seeded articles and media.

### Modified Capabilities
<!-- None. `ushop-site` "Editor field guidance" and "Presentation confined to views and static assets" already apply to new page types and views without a wording change. -->

## Impact

- Content model and seed data under `src/uShopDemo.Web/uSync/v18`: 2 new page types, 2 new templates, 2 new data types, an updated `shop` page type, 11 content items and 12 media items (a folder and 11 images).
- Seeded files under `src/uShopDemo.Web/wwwroot/media/ushop/blog/`.
- Views: `_Layout`, new `blog`, `blogArticle` and author partial. `wwwroot/css/site.css`.
- Application code: a listing and paging helper in `Shop/`, and a wider heading handler. No new packages. Design: `design.md`, ADR-0027, ADR-0028, ADR-0029, ADR-0030.
- Docs: new `docs/blog.md`, plus `README.md`, `docs/content-blocks.md`, `docs/nav-hooks.md` and `docs/feature-requests.md` (FR-5 moved).
- `ushop-demo` tasks 5.1-5.3 should wait until this change is merged.
