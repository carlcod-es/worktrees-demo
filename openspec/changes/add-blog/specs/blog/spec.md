## Purpose

Defines the coffee blog: a paginated listing page in the main navigation, article pages composed from content blocks, single-author content shown on articles, and the seeded articles and images. ADRs: ADR-0027, ADR-0028, ADR-0029 (`docs/adrs/`).

## ADDED Requirements

### Requirement: Blog navigation link
Every Umbraco-rendered page SHALL show a "Blog" link in the main navigation, after the category links, leading to the blog listing page. The link SHALL be marked as current on the listing page and on every article page, in the same way as the current category. On narrow screens the link SHALL be inside the collapsible menu with the category links. The link SHALL NOT be shown when the blog listing page is unpublished.

#### Scenario: Link on every page
- **WHEN** a visitor opens the home, a category, a product, the cart, checkout or a not-found page
- **THEN** the main navigation shows "Blog" after the categories and activating it opens the blog listing page

#### Scenario: Current marker
- **WHEN** a visitor opens the blog listing page or any article
- **THEN** the "Blog" link is marked as current and no category link is

#### Scenario: Mobile menu
- **WHEN** a visitor at 390x844 opens the menu
- **THEN** "Blog" is listed with the categories

### Requirement: Blog listing
The blog listing page SHALL list every published article, ordered by publish date with the newest first. Articles with the same publish date SHALL be ordered by title. Each listed article SHALL show its title as a link to the article, publish date, summary, main image and tags. Publish dates SHALL display as day, full month name and year (for example `28 October 2026`) on the listing and on article pages. With no published articles, the page SHALL say that there are no articles yet and SHALL show no pagination control.

#### Scenario: Newest first
- **WHEN** a visitor opens the blog listing with the seeded articles
- **THEN** the article with the latest publish date is first on page 1 and the one with the earliest is last on page 2

#### Scenario: Order follows the publish date
- **WHEN** an editor changes the oldest article's publish date to the newest and publishes it
- **THEN** that article moves to the top of page 1

#### Scenario: No articles
- **WHEN** every article is unpublished
- **THEN** the listing says there are no articles yet and shows no pagination control

### Requirement: Listing pagination
The blog listing SHALL show 5 articles per page and SHALL render on the server, so that every page works without script. The pagination control SHALL show a link per page, mark the current page to sighted and assistive-technology users, and offer previous and next links except where there is no such page. It SHALL be shown only when there is more than one page. Each page SHALL have its own URL that can be shared. A page value that is missing, not a whole number or less than 1 SHALL show page 1. A page number above the last page SHALL show the last page.

#### Scenario: Two pages
- **WHEN** a visitor opens the listing with 10 published articles
- **THEN** page 1 shows the 5 newest articles and page 2 shows the other 5, with no article on both

#### Scenario: Control state
- **WHEN** a visitor opens page 1 of 2
- **THEN** page 1 is marked current, links to pages 1 and 2 and a next link are shown, and there is no previous link

#### Scenario: Single page
- **WHEN** 5 or fewer articles are published
- **THEN** no pagination control is shown

#### Scenario: Without script
- **WHEN** a visitor follows the link to page 2 with script disabled
- **THEN** page 2 loads with its 5 articles

#### Scenario: Invalid page values
- **WHEN** a visitor opens the listing with a page value of `abc`, `0` or `-1`, or of `99` with 2 pages
- **THEN** the first three show page 1, `99` shows page 2, and each response is successful

### Requirement: Article page
Each article SHALL have its own page showing, in order: the main image as a banner, a link back to the listing, title, publish date, tags, summary, main content and the author section (requirement "About the author"). The banner SHALL be directly under the site header and SHALL fill the width of the page content, matching the home page banner. On narrow screens it MAY crop at the sides but SHALL keep its subject visible. The main image SHALL use its media alt text, or the article title when it has none.

#### Scenario: Open an article
- **WHEN** a visitor activates an article's title on the listing
- **THEN** the article page shows each of these elements in that order

#### Scenario: Banner at the top
- **WHEN** a visitor opens an article at 1280x720
- **THEN** the main image is the first thing under the site header, is wider than it is tall, and is as wide as the home page banner

#### Scenario: Banner on a phone
- **WHEN** a visitor opens an article at 390x844
- **THEN** the banner fills the content width, its subject is visible and nothing overflows horizontally

#### Scenario: Image without alt text
- **WHEN** an editor clears the alt text of an article's main image
- **THEN** the article page uses the article title as the image's alt text

### Requirement: Article fields
Title, publish date, summary, main image and main content SHALL be required to publish an article. Editors SHALL be able to set the publish date, which SHALL NOT change when an article is edited or republished. The summary SHALL be plain text; markup typed into it SHALL be shown literally.

#### Scenario: Required field missing
- **WHEN** an editor tries to publish an article with no summary, main image, publish date or main content
- **THEN** publishing is rejected with a message saying what is missing

#### Scenario: Republish keeps the date
- **WHEN** an editor changes an article's summary and publishes it
- **THEN** its publish date and its position in the listing are unchanged

#### Scenario: Markup in summary
- **WHEN** an editor saves the summary `Fresh <b>beans</b>` and publishes
- **THEN** the listing and article page show the text `Fresh <b>beans</b>` literally

### Requirement: Article tags
An article SHALL have between 0 and 3 tags, entered as short text by editors and shown as text labels on the listing and the article page. Tags SHALL NOT be links. An article with no tags SHALL show no tag area. Publishing an article with more than 3 tags SHALL NOT be possible.

#### Scenario: Three tags
- **WHEN** an article has 3 tags
- **THEN** the listing and article page show all 3 as labels that are not links

#### Scenario: Limit
- **WHEN** an editor tries to add a fourth tag and publish
- **THEN** the fourth tag cannot be added, or publishing is rejected with a message giving the limit

#### Scenario: No tags
- **WHEN** an editor removes every tag from an article and publishes
- **THEN** the listing and article page show no tag area for that article

### Requirement: Article main content
An article's main content SHALL be composed from the same block types as a product description (`ushop-site` requirement "Product description blocks"): rich text, feature list and quote, with at least one block. Rich text in an article SHALL follow the same formatting rules (`ushop-site` requirement "Rich-text formatting"), including heading levels.

#### Scenario: Blocks in order
- **WHEN** an article has a rich-text block, a quote and a feature list
- **THEN** the article page shows the three blocks in that order

#### Scenario: Heading level on save
- **WHEN** an article's rich-text block is saved with an h1 and an h4
- **THEN** they are stored and shown as h2 and h3

### Requirement: About the author
The blog SHALL have one author. The author's name, photo and bio SHALL be edited once, in a separate "About the author" tab on the blog listing page in the backoffice, and SHALL be shown at the foot of every article page, above the site footer, under the heading "About the author". There SHALL be no author page. The listing page and all other site pages SHALL NOT show the section. Name and photo are optional. When the bio is empty, the section SHALL NOT be shown. The bio SHALL be plain text with line breaks kept; markup typed into it SHALL be shown literally.

#### Scenario: Edit once, shown everywhere
- **WHEN** an editor changes the bio on the blog listing page and publishes it
- **THEN** every article page shows the new bio without the articles being republished

#### Scenario: Only on articles
- **WHEN** a visitor opens the blog listing, the home page or a product page
- **THEN** no "About the author" section is shown

#### Scenario: Empty bio
- **WHEN** an editor clears the bio and publishes
- **THEN** article pages show no "About the author" section

#### Scenario: No photo
- **WHEN** an editor removes the author photo and publishes
- **THEN** article pages show the name and bio with no empty image space

### Requirement: Seeded blog
On first boot against an empty database, the site SHALL import the blog listing page, the author content and 10 published articles without manual steps. The import SHALL be idempotent. Articles SHALL be humorous and coffee themed, written in one author's voice, with distinct publish dates in the past. People and organisations named in them SHALL be fictional. Each article SHALL have a summary, a distinct main image and at least one rich-text block of at least 150 words. At least 3 articles SHALL also contain a feature list and a quote. At least one article SHALL have 1 tag and at least one SHALL have 3. Every main image SHALL be a wide landscape banner. The author photo SHALL show a gender-neutral coffee nerd, and the seeded author content SHALL NOT assign the author a gender. Every main image and the author photo SHALL be a backoffice-managed SVG media item with its own alt text (ADR-0020).

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** the listing shows 2 pages of 5 articles, each article page loads, and each image file loads

#### Scenario: Second start on same database
- **WHEN** the site restarts against an already seeded database
- **THEN** no duplicate articles or media are created

#### Scenario: Media library
- **WHEN** an editor opens the media section
- **THEN** a blog folder holds each article image and the author photo, each with a non-empty alt text

### Requirement: Blog layout independence
Blog appearance SHALL be defined only by views and static CSS, and the listing and paging rules SHALL NOT be defined in views, so that a layout can restyle the blog without changing which articles appear on which page. Listing page, article page and author section markup SHALL each be in a separate view that a layout can override.

#### Scenario: Restyled blog
- **WHEN** a layout changes only the blog views and CSS
- **THEN** the same articles appear on the same pages in the same order

### Requirement: Readable blog on small and large screens
Blog listing and article pages SHALL have no horizontal scrolling at 390x844 or 1280x720. Pagination links SHALL be at least 44px tall on narrow screens.

#### Scenario: Phone
- **WHEN** a visitor opens the listing and an article at 390x844
- **THEN** nothing overflows horizontally and each pagination link is at least 44px tall
