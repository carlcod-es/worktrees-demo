# ADR-0027: Blog as a listing page with child articles, author content on the listing

- Status: Accepted
- Date: 2026-11-04
- Source: `openspec/changes/add-blog/design.md`; Product Owner constraints (Block List for article content; author content in a tab on the listing page)
- Extends: ADR-0025 (handler scope), ADR-0023 (block types reused)

## Context

The `blog` spec needs a listing page, articles built from blocks, and one author with no author page, shown on every article. The model must seed through uSync (ADR-0006, ADR-0020) and stay the same across layouts (ADR-0007). Published content is in-memory, and there are few articles.

## Decision

- New page types: `blog` (one, under the shop root) and `blogArticle` (children of `blog`). Article URLs are `/blog/<article-name>/`.
- `publishDate` is an editor-set date property. It decides the listing order.
- Author fields live on `blog` in an "About the author" tab. An article reads them from its parent, so an edit needs no article republish.
- `blogArticle.mainContent` uses a new `Article blocks` Block List data type with the existing three element types and at least one block. Existing block views and hand-authored seed JSON are reused without change.
- The heading handler (ADR-0025) also covers `blogArticle.mainContent`.

## Alternatives

- Reuse the `Product description blocks` data type: one fewer file, but a misleading name, and a later product-only change would alter articles too. It also has no minimum, and articles need one block.
- Separate author content node or settings page: a cleaner fit for many authors, but adds a page type and a picker for one author, and the Product Owner asked for a tab on the listing page.
- Author fields on each article: repeats content and fails "edit once".
- Sort by Umbraco create or update date: seeded dates would all be the import time, and an edit could move an article.
- Model the blog as a `category`: it would then list as a shop category and carry product rules.

## Consequences

- If `blog` is unpublished, its articles have no published parent, so the nav link and articles disappear together.
- More than one author would need a new ADR and a content change.
- New block types need adding to two data types if both pages should offer them.
