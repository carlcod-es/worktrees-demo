Block rendering, block JSON round-trips and block editor behaviour are already proven (`add-product-blocks` task 1.1) and aren't re-tested here. No section needs a spike. Sections 1 and 3 touch disjoint files and can run in parallel. Backoffice checks use the Management API or a person; no browser automation.

## 1. Blog artwork (worker-frontend; explicit assignment of `src/uShopDemo.Web/wwwroot/media/ushop/blog/**` and `openspec/changes/add-blog/media-manifest.json`)

Contract for section 2: `media-manifest.json`, per `design.md` "Seeded media".

- [ ] 1.1 Create the 10 article SVGs and `author.svg` named in `design.md` "Seeded articles", matching the style of `wwwroot/media/ushop/categories/coffee.svg`, and write `media-manifest.json` with each file and its alt text. Verify with a Node one-liner: 11 files; the 10 article files have `viewBox="0 0 1600 500"` with the subject inside the central 640x460, `author.svg` has `viewBox="0 0 800 600"`, and none has `<script`, `href="http` or `xlink:href`; each alt text is non-empty and distinct; every manifest path exists.

## 2. Content model and seed (worker-backend; explicit assignment of `src/uShopDemo.Web/uSync/**`)

Depends on 1.1. Contract for sections 3 and 4: `design.md` "Content model contract".

- [ ] 2.1 Add data types `Article blocks` and `Article tags`, templates `blog` and `blogArticle`, page types `blog` and `blogArticle` with descriptions from `design.md` "Editor guidance text", and allow `blog` under `shop`. Verify: a fresh `node scripts/dev.mjs` import reports no failed items; `grep` finds no empty `<Description>` in the new page types; any stub views the import wrote are removed.
- [ ] 2.2 Add the `Blog` media folder and 11 media items from `media-manifest.json`, with `umbracoBytes` matching file sizes. Verify: fresh import has no failed items; through the Management API, the folder has 11 items, each with non-empty alt text; each file URL returns 200.
- [ ] 2.3 Add the `Blog` content node (title "The Daily Grind", `SortOrder` after the existing children) with Perry Kolator's name, photo and bio, and the 10 articles from `design.md` "Seeded articles": fixed GUIDs, listed dates, tags and blocks, humorous copy at least 150 words per rich-text block, fictional names only, H2 and H3 headings only. Verify: fresh import has no failed items; through the Management API, there are 10 published articles with the specified dates and tag counts; a Node word count over the seed files shows each first block at 150 words or more; a restart against the same database creates no duplicates.

## 3. Listing helper and heading rule (worker-backend; own paths `src/uShopDemo.Web/Shop/**`)

Depends on the aliases in `design.md` "Content model contract", not on section 2 files. Contract for section 4: `design.md` "Listing helper".

- [x] 3.1 Add `Shop/Blog/BlogListing.cs` per `design.md` "Listing helper" and ADR-0029. Verify: `dotnet build uShopDemo.slnx` succeeds; the reviewer reads `Clamp` against `null`, `""`, `abc`, `0`, `-1`, `1.5`, a value above `int.MaxValue`, 99 and zero items. Behaviour is checked through pages in task 4.2.
- [ ] 3.2 Extend the heading handler to `blogArticle.mainContent` per `design.md` "Heading normaliser". Verify: `dotnet build uShopDemo.slnx` succeeds; after section 2, on a fresh run, saving one article via the Management API with `<h1>A</h1><h4>B</h4>` in a rich-text block stores `<h2>A</h2><h3>B</h3>`. This checks the new wiring only. Depends on section 2 for the check.

## 4. Blog views (worker-frontend)

Depends on sections 2 and 3.

- [ ] 4.1 Add the Blog link and "Main" label to `Views/Shared/_Layout.cshtml`, then `Views/blog.cshtml`, `Views/blogArticle.cshtml` and the partials `blogCard`, `blogPagination` and `aboutAuthor`, per `design.md` "Rendering". Verify: `dotnet build uShopDemo.slnx` succeeds; `[data-nav]`, `[data-nav-toggle]` and `[data-nav-menu]` are unchanged.
- [ ] 4.2 Add blog styles to `wwwroot/css/site.css`, per `design.md` "Styling". Verify on a fresh `node scripts/dev.mjs`, by fetching pages and reading served files (no browser):
  - home, category, product, cart, checkout and 404 pages all have the Blog link after the categories, and none has an author section;
  - `/blog/` lists the five articles dated 2026-10-28 to 2026-09-30, in that order, with image, date, summary and tags; `/blog/?page=2` lists the other five; `aria-current="page"` is on the right link; page 1 has no previous link and page 2 has no next link;
  - `?page=abc`, `0` and `-1` return page 1, and `99` returns page 2, each with status 200;
  - every article returns 200, with elements in the spec's order (the banner first, under the header), its tags as text with no links, a non-empty image alt and one author section; the Blog nav link has `active` on the listing and articles;
  - article 8 has one block type and article 1 has all three, in the seeded order;
  - the CSS has the 44px rule for pagination links below 48rem, and `.article-banner` has no fixed width and uses `aspect-ratio: 16 / 7` below 48rem.

## 5. Docs (Architect; explicit paths)

Depends on section 4.

- [ ] 5.1 Add `docs/blog.md` (content model, editor workflow, URLs, markup contract, what a layout may change). In `README.md`, mention the blog and link the doc. In `docs/content-blocks.md`, note that article content uses the same block views. In `docs/nav-hooks.md`, change the example's `aria-label` to "Main". Move FR-5 out of `docs/feature-requests.md`. Add a `docs/follow-ups.md` entry for `BlogListing` tests. At archive, the new `blog` spec already lists ADR-0027, ADR-0028 and ADR-0029. Verify: links resolve.

## 6. Verification (Product Owner, Firefox)

Depends on sections 1-5.

- [ ] 6.1 On a fresh `node scripts/dev.mjs`, check each `blog` spec scenario that the fetch checks don't cover: visual review of listing and articles at 1280x720 and 390x844 with no horizontal scroll; Blog in the open mobile menu; page 2 with script disabled; in the backoffice, the "About the author" tab, a fourth tag blocked, required-field rejection, a bio edit showing on articles without republishing them, an empty bio and no photo; publish-date reorder; and a read-through of the article copy.
