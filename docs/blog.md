# Blog

"The Daily Grind": a coffee blog with a paginated listing, articles built from blocks, and one author. Requirements: `blog` spec. Decisions: ADR-0027, ADR-0028, ADR-0029, ADR-0030. Seeded by uSync on first boot (ADR-0006).

## URLs

| Page | URL |
|---|---|
| Listing, page 1 | `/the-daily-grind/` |
| Listing, page N | `/the-daily-grind/?page=N` |
| Article | `/the-daily-grind/<article-name>/` |

URLs come from node names (ADR-0030), so views use `Url()` and never hard-code a path. Renaming the node changes its URLs and nav label. ADR-0027 and ADR-0029 still show `/blog/`; ADR-0030 supersedes those clauses.

A `page` that is missing, not a whole number (including values too large for `int`) or below 1 shows page 1. A number past the last page shows the last page. All return 200.

## Content model

| Type | Tab | Alias | Data type | Required to publish |
|---|---|---|---|---|
| `blog` | Content | `title` | Textstring | no; heading falls back to the node name |
| `blog` | About the author | `authorName` | Textstring | no |
| `blog` | About the author | `authorImage` | SVG Image | no |
| `blog` | About the author | `authorBio` | Textarea | no; empty hides the author section |
| `blogArticle` | Content | `title` | Textstring | yes |
| `blogArticle` | Content | `publishDate` | Date Picker | yes |
| `blogArticle` | Content | `summary` | Textarea | yes |
| `blogArticle` | Content | `mainImage` | SVG Image | yes |
| `blogArticle` | Content | `tags` | Article tags (0-3) | no |
| `blogArticle` | Content | `mainContent` | Article blocks (1 or more) | yes |

`blog` sits under the shop root and allows only `blogArticle` children. Blocks: `docs/content-blocks.md`.

## Editor workflow

- **Add an article:** create a `Blog article` under The Daily Grind, fill in the fields and publish. Required fields are enforced on publish, not on save.
- **Order:** `publishDate` sets listing order, newest first, with same-day articles by title. It doesn't change on edit or republish. A future date doesn't hide an article.
- **Images:** upload an SVG to the `Blog` media folder with alt text. Banners are 1600x500 with the subject in the central 640x460, because narrow screens and listing cards crop the sides.
- **Tags:** display-only text, not links. Spelling isn't enforced (ADR-0028).
- **Author:** edit the "About the author" tab on The Daily Grind and publish. Every article picks it up without being republished.
- **Keeping changes:** the database is empty on each run (ADR-0005), so backoffice changes are lost unless exported to uSync. See `README.md` (ADR-0017, ADR-0020).

## Markup contract

| View | Renders |
|---|---|
| `Views/Shared/_Layout.cshtml` | The `blog` child as the last link in `[data-nav-menu]`, labelled with its `Name`, with `.active` on the listing and articles. Omitted when the node is unpublished. |
| `Views/blog.cshtml` | `h1`, then `.blog-list` of cards and pagination, or `p.empty` with no articles. |
| `Views/Partials/blogCard.cshtml` | `article.blog-card`: image link (`alt=""`, hidden from assistive technology), `.blog-card-body` with `h2 > a`, `.article-meta > time`, `.summary`, `ul.tag-list > li.tag`. |
| `Views/Partials/blogPagination.cshtml` | `nav.pagination[aria-label="Blog pages"]`. Model: `BlogPage`; it takes the listing URL from the first article's parent, so render it only when `TotalPages > 1`. `aria-current="page"` on the current link, with a visible style. Previous and Next only where they exist. |
| `Views/blogArticle.cshtml` | In order: `img.article-banner`, `.breadcrumb`, then `article.article` with `h1`, `.article-meta > time`, `ul.tag-list`, `.article-summary`, `.description.article-body` with the blocks. Block CSS needs the `.description` wrapper. Then the author partial, passed `Model.Parent()`. |
| `Views/Partials/aboutAuthor.cshtml` | `aside.about-author[aria-labelledby]` with an `h2`, optional photo and name, and `.about-author-bio`. Model: the `blog` node. Nothing when the bio is empty. |

Plain-text values go through Razor encoding, so never use `Html.Raw` on them. The bio keeps line breaks with `white-space: pre-line`. Dates are `<time datetime="yyyy-MM-dd">` displaying `d MMMM yyyy` in `en-GB`.
