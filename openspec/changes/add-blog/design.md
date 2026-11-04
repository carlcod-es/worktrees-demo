## Context

- Umbraco 18.3, uSync 18.2, `ModelsMode: Nothing`: views read properties by alias (ADR-0002). The content model and seed data are uSync files imported on first boot (ADR-0006), and the database is ephemeral (ADR-0005).
- Reusable as is: the three block element types, `Views/Partials/blocklist/**`, block CSS scoped under `.description`, the `SVG Image` picker, `Date Picker`, `Textarea` and `Textstring` data types, and SVG media seeding (ADR-0020).
- `_Layout.cshtml` builds the nav from `category` children and marks the current item by comparing `Model.AncestorOrSelf(2)`. That also works for `blog` and its articles.
- `shop.cshtml` already filters children to `category`, so the home page is unaffected.
- `ProductHeadingNormalisationHandler` only checks `product.description`. `HeadingNormaliser` itself works on any Block List JSON containing `richTextBlock`.
- The Product Owner has confirmed the block list works, so this change doesn't re-spike or re-test block rendering, labels, editor behaviour or JSON round-trips (`add-product-blocks` task 1.1).
- `add-mobile-nav` isn't archived yet, so its requirement isn't in the main `ushop-site` spec. The `blog` spec therefore adds its own navigation requirement.

Requirements: `specs/blog/spec.md`. Motivation: `proposal.md`. ADRs: ADR-0027, ADR-0028, ADR-0029.

## Goals / Non-Goals

**Goals:**
- Page types, seed content and views as defined below, with no new packages.
- Listing and paging rules in one helper, so layouts only render the result (ADR-0029).

**Non-Goals:**
- Items in `proposal.md` "What Changes", out of scope.
- A web test project. `docs/follow-ups.md` already records the need for one; a new `*.csproj` is scope growth. Paging edge cases are checked by fetching pages. Log it as a follow-up against the helper too.
- Hiding articles with a future publish date. `publishDate` only sets order and display.
- Canonical links or other SEO markup.

## Decisions

### Content model contract

`add-blog` tasks sections 2, 3 and 4 are built against these aliases. Rationale and alternatives: ADR-0027, ADR-0028.

| Page type | Alias | Icon | Parent | Template |
|---|---|---|---|---|
| Blog | `blog` | `icon-newspaper` | `shop` (add to its allowed children) | `blog` |
| Blog article | `blogArticle` | `icon-article` | `blog` (only allowed child) | `blogArticle` |

| Type | Tab | Property alias | Name | Data type | Mandatory |
|---|---|---|---|---|---|
| `blog` | Content | `title` | Title | Textstring | no |
| `blog` | About the author | `authorName` | Author name | Textstring | no |
| `blog` | About the author | `authorImage` | Author photo | SVG Image | no |
| `blog` | About the author | `authorBio` | Author bio | Textarea | no |
| `blogArticle` | Content | `title` | Title | Textstring | yes |
| `blogArticle` | Content | `publishDate` | Publish date | Date Picker | yes |
| `blogArticle` | Content | `summary` | Summary | Textarea | yes |
| `blogArticle` | Content | `mainImage` | Main image | SVG Image | yes |
| `blogArticle` | Content | `tags` | Tags | Article tags | no |
| `blogArticle` | Content | `mainContent` | Main content | Article blocks | yes |

Both `blog` tabs and the `blogArticle` tab use `Type` `Tab`, not `Group`, so `blog` shows two tabs with no "Generic" tab.

New data types:

| Name | Editor | Config |
|---|---|---|
| Article blocks | `Umbraco.BlockList` | Same three element types and labels as `Product description blocks`; `validationLimit.min` 1, max null; inline and live editing off; `createModalSize` medium |
| Article tags | `Umbraco.MultipleTextstring` | min 0, max 3 |

Seed values use the same shapes as existing content: Block List JSON as in `Content/brewing-scale.config`, a media-picker array as `images`, and newline-joined tags as Feature items.

### Rendering

| View | Owner of rules | Notes |
|---|---|---|
| `Views/Shared/_Layout.cshtml` | - | Adds the `blog` child as the last link inside `[data-nav-menu]`. `aria-label` becomes "Main". Hooks unchanged (`docs/nav-hooks.md`). |
| `Views/blog.cshtml` | `BlogListing` | Heading, cards, pagination, empty message. |
| `Views/Partials/blogCard.cshtml` | - | One card. Image `alt=""` and a hidden link, as `productCard.cshtml`, since the title link has the name. |
| `Views/Partials/blogPagination.cshtml` | - | `<nav aria-label="Blog pages">`, links, `aria-current="page"` on the current one. |
| `Views/blogArticle.cshtml` | - | Elements in the spec's order. The main image renders first as `<img class="article-banner">`. Blocks use `Html.GetBlockListHtmlAsync` in `<div class="description article-body">`, reusing block CSS. |
| `Views/Partials/aboutAuthor.cshtml` | - | Model: the `blog` node. `<aside aria-labelledby=...>` with an `<h2>`, or nothing when the bio is empty. |

Dates render as `<time datetime="yyyy-MM-dd">` with `d MMMM yyyy` and an explicit `en-GB` culture, since the site language remains `en-US`. Plain-text values are output through Razor encoding. The bio keeps line breaks with `white-space: pre-line`.

Markup contract for layouts goes in `docs/blog.md`.

### Listing helper

ADR-0029. Contract for `add-blog` tasks section 4, in `src/uShopDemo.Web/Shop/Blog/BlogListing.cs`:

```csharp
public sealed record BlogPage(IReadOnlyList<IPublishedContent> Articles, int CurrentPage, int TotalPages);

public static class BlogListing
{
    public const int PageSize = 5;
    public static BlogPage ForPage(IPublishedContent blog, string? rawPage);
    public static (int Page, int TotalPages, int Skip) Clamp(int totalItems, string? rawPage, int pageSize = PageSize);
}
```

`Clamp` is pure. Zero items gives page 1 of 1, so the control stays hidden with `TotalPages > 1`. Values too large for `int` count as not a whole number and show page 1. The view passes `Context.Request.Query["page"]`.

### Heading normaliser

Replace the two handler constants with a small alias map: `product` to `description`, and `blogArticle` to `mainContent`. `HeadingNormaliser` doesn't change. Renaming the handler is optional and not worth churn. The comments that say "product description" get updated.

### Seeded media

Follows ADR-0020. Files live in `wwwroot/media/ushop/blog/`, with shapes only and no scripts or external references. Article banners are 1600x500, with the subject inside the central 640x460 so that crops keep it visible, and background decoration across the rest of the width. `author.svg` stays 800x600, because it displays small and isn't a banner. It shows a gender-neutral, centred coffee nerd. uSync media items go in a new `Blog` root folder (`SortOrder` 3). `umbracoBytes` matches each file's real size. `openspec/changes/add-blog/media-manifest.json` lists each file with its alt text, as `add-shop-media` did, and is the contract between `add-blog` tasks sections 1 and 2.

### Seeded articles

Newest first. Page 1 holds the first five. Slugs follow from the node name. Author: Perry Kolator, a fictional coffee obsessive with a dry voice. The bio and any first-person copy give Perry no gender and use no gendered pronouns.

| # | Title | Publish date | Tags | Image | Extra blocks |
|---|---|---|---|---|---|
| 1 | Your Grinder Is Judging You | 2026-10-28 | Grinders, Humour | `grinder-judging.svg` | feature list, quote |
| 2 | A Field Guide to the Coffee Bore | 2026-10-21 | Culture, Humour | `field-guide.svg` | feature list |
| 3 | Bloom: Thirty Seconds of Staring at Wet Coffee | 2026-10-14 | Brewing, Pour-over | `bloom.svg` | quote |
| 4 | The Moka Pot Will Tell You When It's Done. So Will the Smoke Alarm. | 2026-10-07 | Brewing, Moka pot, Humour | `moka-alarm.svg` | feature list, quote |
| 5 | How Many Mugs Is Too Many Mugs? | 2026-09-30 | Mugs | `too-many-mugs.svg` | feature list |
| 6 | Tasting Notes, Translated | 2026-09-23 | Tasting, Beans | `tasting-notes.svg` | feature list, quote |
| 7 | In Defence of French Press Sludge | 2026-09-16 | Brewing, French press | `press-sludge.svg` | quote |
| 8 | I Weighed My Coffee for a Month and Now I Weigh Everything | 2026-09-09 | Scales, Humour | `weigh-everything.svg` | none |
| 9 | The Gooseneck Kettle: Pouring With Unearned Confidence | 2026-09-02 | Kettles, Pour-over, Humour | `gooseneck.svg` | quote |
| 10 | Decaf: A Love Story With One Participant | 2026-08-26 | Beans | `decaf.svg` | none |

Every article has a rich-text block of at least 150 words first, using at least one H2 heading. Quotes are attributed to fictional people. Article 1, 4 and 6 meet the spec's "feature list and quote" minimum. Author photo: `author.svg`.

### Editor guidance text

Required by `ushop-site` "Editor field guidance".

| Item | Description |
|---|---|
| Blog (type) | The blog listing page; one per site, under the shop root. Articles go under it. |
| Blog `title` | Heading on the blog listing page. |
| Blog `authorName` | Optional. Author's name, shown at the foot of every article. |
| Blog `authorImage` | Optional. Author's SVG photo, shown at the foot of every article. |
| Blog `authorBio` | Plain text, shown at the foot of every article. Leave empty to hide the author section. |
| Blog article (type) | A blog post. Has its own page and a card on the blog listing. |
| Article `title` | Article name. Shown as the page heading and on its listing card. |
| Article `publishDate` | Sets the listing order, newest first, and is shown with the article. Not changed by later edits. |
| Article `summary` | One or two sentences of plain text. Shown on the listing card and under the article heading. |
| Article `mainImage` | Wide SVG banner, 1600x500, with its subject in the centre. Shown at the top of the article and cropped on the listing card. |
| Article `tags` | Optional. Up to 3 short labels, shown on the card and article. They don't link anywhere. |
| Article `mainContent` | Blocks shown below the summary, in this order. Add at least one. |

### Styling

Adds a responsive card list, tag labels, a pagination row and an author panel to `site.css`, using the existing custom properties and `(width < 48rem)` breakpoint. Listing cards are full-width rows with the image to the left of the text on wide screens and above it on narrow screens, because a card's summary needs more reading width than a product card. The article banner is the first element in `.site-main`, at the full `.wrap` content width with `--radius` corners, matching `.hero`. It shows at 1600:500 on wide screens. Below 48rem it uses `aspect-ratio: 16 / 7` with `object-fit: cover`, because 1600:500 would be only about 110px tall on a phone. Listing card images use `object-fit: cover` at 4:3, showing the central subject. Article text is capped at about 42rem for reading comfort. `.article-body` removes the `.description` top border.

## Risks / Trade-offs

- [uSync may write an empty view file when it imports a template with no view (`add-blog` task 2.1)] → Delete any stub so `add-blog` task 4.1 creates the real view.
- [Article pages return 500 until section 4 lands] → Section 2 is verified through the import results and backoffice data, not by rendering pages.
- [`validationLimit.min` or mandatory rules apply on publish, not on save (seen in `add-product-blocks` task 1.2)] → The spec only requires rejection on publish.
- [Long article titles overflow on phones] → Listed titles wrap with `overflow-wrap: anywhere`. Article 4 is the long-title test.
- [Tag spelling drifts] → Accepted for display-only tags (ADR-0028).
- [Layout branches restyle the header and could lose the Blog link] → None are cut yet. `ushop-demo` tasks 5.1-5.3 follow this merge, and `docs/blog.md` names the link.

## Migration Plan

Merge to `main` before `ushop-demo` tasks 5.1-5.3. Fresh databases pick it up on first boot. Rollback means reverting the section commits.
