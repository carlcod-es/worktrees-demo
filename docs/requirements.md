# Requirements

What the app must do. Headings use the capability names that ADRs cite as specs, such as the `blog` spec. Implementation detail is in `docs/implementation-notes.md`.

Out of scope: payments, stock, tax, shipping, members or logins, emails, an admin order UI, production hardening, and data that persists across runs.

## `aspire-orchestration`

ADR-0003, ADR-0004, ADR-0005.

- The AppHost defines the whole stack: one SQL Server container, one database shared by the site and the cart API, the site and the API. Nothing outside it is needed.
- The API waits for the database. Its health check passes only when the database is reachable and migrated.
- The site waits for the database and doesn't start until the API is healthy.
- The site receives the API's public URL, and the API receives the site's origin. A page calls the API of its own AppHost.
- Each instance has its own SQL container and database, with no volume. A restart recreates the database empty and re-seeds it.
- No port is fixed across worktrees. Dashboard, OTLP, resource service, site and API ports can be configured; the SQL port is dynamic.
- Started by the launcher, every endpoint uses HTTPS on `localhost` with the trusted dev certificate, in every worktree. No endpoint accepts plain HTTP. The dashboard opens without a login token.

## `worktree-launcher`

ADR-0004.

- `scripts/dev.mjs` derives a port block from the worktree path, so ports are the same on every run. If a port is taken, it moves to the next free block and prints the result.
- It prints the site, API and dashboard URLs, branch and path before starting, passes ports and branch to the AppHost, and runs in the foreground until interrupted.
- `scripts/worktrees.mjs` creates worktrees for the theme branches, lists them with their URLs, and removes them. `up` creates missing branches from the branch checked out in the main worktree. `down` keeps the branches.
- Scripts use only Node built-ins and path APIs, with no shell-specific syntax, and behave the same on Windows, macOS and Linux.

## `cart-api`

ADR-0010, ADR-0011, ADR-0012, ADR-0014, ADR-0016, ADR-0018.

**Service**
- Runs as its own process on its own HTTPS endpoint, and keeps working if the site stops.
- Stores data in `ushop`-prefixed tables in the instance's database, with its own migrations history table. Neither the API nor the site touches the other's tables.
- Migrations alone manage the tables, applied at startup before the first request.

**Sessions**
- A session is a random id of at least 128 bits, signed to detect tampering, in a cookie that is HttpOnly, Secure, SameSite=Strict, host-only and path `/`. The name includes the API port, so instances on one host don't overwrite each other.
- The session endpoint returns the existing session if the cookie is valid, otherwise issues a new one. It returns 403 and sets no cookie for a request from another site's origin.
- Sessions last 30 days from last renewal and survive an API restart.
- Cart, operation and order endpoints return 401 without a valid session, and queue nothing.
- Session ids never appear in response bodies or logs.

**Carts and ownership**
- Carts are addressed by a caller-supplied GUID. With a valid session, a cart never written reads as empty with 200.
- A cart belongs to the session of its first accepted write. Another session reads it as empty, gets 404 on writes and queues nothing.
- Orders and operations are readable only by the session that created them; others get 404, identical to an unknown id.
- If two sessions race to create a cart id, one owns it and the other's operation fails.

**Writes**
- Add, change quantity, remove and checkout are validated synchronously, queued, and answered 202 with an operation id and a `Location` header. Only background processing changes the database. Writes to one cart apply in the order accepted.
- Returns 400 with validation errors, and queues nothing, for quantity below 1 (add) or 0 (update), above 99, a missing product key, name or SKU, or a negative price.
- One line per product key. Adding again increases quantity, capped at 99. Quantity 0 removes the line. Name, SKU and unit price are stored as first supplied and not changed by later adds.
- Product data comes from the caller and isn't checked against the catalogue. This is a deliberate demo simplification, stated in the code and `README.md`.
- Operation status is `queued`, `completed` or `failed` with a reason, available for at least 10 minutes while the process runs. An unknown id returns 404.
- A cart read returns open lines (product key, name, SKU, unit price, quantity, line total), item count and grand total.

**Checkout and orders**
- Requires name, valid email, address and a non-empty card number. The card number is never stored or processed.
- Acceptance returns 202 with an order number. Processing converts the cart to an order with customer details, time and total, and the cart id then reads as empty. An empty cart is rejected with a reason.
- An order reads `pending`, `confirmed` (with customer name, email, address, time, lines and total) or `rejected` with a reason. A number never issued returns 404.

**Browser access**
- Cross-origin requests with credentials are allowed only from the site's origin.
- Write requests with a body need `Content-Type: application/json`, otherwise 415, so every cross-origin write needs a preflight.

**API docs**
- In Development only, a machine-readable description of every endpoint and a browsable reference page, both without a session. A developer can start a session and call the cart endpoints from that page. In other environments both return 404.

## `ushop-site`

ADR-0002, ADR-0006, ADR-0007, ADR-0015, ADR-0019 to ADR-0026.

**Install and seed**
- Umbraco installs unattended on first boot with a known backoffice login and no installer UI.
- First boot imports, idempotently and with no manual steps: at least 4 categories, each with an image and intro, and at least 12 products, each with name, SKU, price, summary, description and 3-4 images.
- Every seeded product has a rich-text block, and at least 2 also have a feature list and a quote.

**Media (ADR-0020, ADR-0022)**
- Product and category images and the logo are backoffice media items stored as SVG, each seeded with its own alt text. Editors can upload an SVG.
- Alt text is editable and not required to save. A gallery image without it uses `<product title>, image <n>`.

**Pages**
- A category page lists only its products, with name, price, first image and link. Product cards show the first gallery image.
- A product page shows name, SKU, price, description blocks, gallery and add to cart with quantity.
- Adding calls the API from the browser with no reload, shows progress, then updates the header count. If the API is unreachable, the page shows an error and the count doesn't change.
- Gallery: one main image and a thumbnail per image, in editor order, each a distinct view of the product. Choosing a thumbnail by pointer or keyboard swaps the image with no reload, uses that image's alt text, marks the thumbnail current for sighted and assistive-technology users, and keeps focus on it. Without script, the first image shows and each thumbnail links to its full image. Editors can add, remove and reorder images.

**Cart and checkout**
- The cart id is stored in the browser, and the cart is held by the API, reachable only from the session that created it. The page gets a session before its first cart request. Another browser given the same id sees an empty cart.
- Visitors add, change quantity and remove lines and see line and grand totals, without reloads. The header count loads from the API on every page. An empty cart says so, and checkout is unavailable.
- Checkout collects name, email, address and a fake card number and submits them from the browser. No payment happens. Validation errors show beside the form with no reload.
- The confirmation page shows the order as processing, then the number, lines and total, or the rejection reason.

**Errors (ADR-0015)**
- Friendly pages in every environment, with no exception details. Unknown URLs return 404 with an Umbraco page in the shared layout. Unhandled exceptions return 500 with a static page. Other 4xx keep their status with a static page.
- Static pages have no branch badge and show a static copy of the logo linked home. `/umbraco` keeps Umbraco's own error handling.

**Logo**
- The shop root has a logo, seeded as an SVG wordmark. Every Umbraco-rendered header shows it as a link home, with the site title as alt text. With no logo, the header shows the site title as a text link.

**Presentation**
- Appearance comes only from Razor views and static CSS and assets. Markup rendered from API data comes from templates in the views.
- Every page shows the current git branch in a small badge.
- Prices show as `£1,234.56` everywhere, formatted identically by server and script. Stored prices don't change, and the site language stays `en-US`.

**Editing (ADR-0023, ADR-0024, ADR-0025)**
- A product description is an optional Block List of rich text (content required), feature list (optional heading, one or more items) and quote (text required, optional attribution). Editors add, edit, reorder, repeat and remove blocks with no limit. With no blocks, the page shows no description area.
- In the editor, each block is a row with an icon, type name and content snippet, with no styled preview. Each block type renders in its own view.
- Rich text allows only bold, italic, links, bulleted and numbered lists and two heading levels. Tables, images, embeds, colours and fonts aren't available or rendered, including when pasted.
- A category intro is required plain text of at most 160 characters. Typed markup shows literally.
- Every page type, page-type property and block property has an editor description of one or two short sentences, saying where it's used and what to enter.

**Mobile navigation (ADR-0026)**
- Below 768px, the category links sit behind a "Menu" button, with the logo and cart link still visible. The button toggles the links by pointer or keyboard with no reload and exposes its expanded state. Escape closes the menu and moves focus to the button. The current category stays marked.
- At 768px and wider, or without script, the links show and the button doesn't.
- Nothing overflows horizontally at 390x844.

## `blog`

ADR-0027 to ADR-0030. Content model and markup are in `docs/blog.md`.

**Navigation**
- Every Umbraco-rendered page links to the listing after the categories, labelled with the listing's name, seeded as "The Daily Grind". It's marked current on the listing and on articles, sits in the mobile menu, and is hidden when the listing is unpublished.

**Listing**
- Lists every published article, newest first, with same-date articles by title. Each shows a title link, date, summary, main image and tags. Dates read like `28 October 2026`.
- With no articles, it says so and shows no pagination.
- 5 per page, rendered on the server, so every page works without script and has its own shareable URL. The control has a link per page, marks the current one for sighted and assistive-technology users, offers previous and next where they exist, and shows only with more than one page.
- A page value that is missing, not a whole number or below 1 shows page 1. One above the last shows the last page. All succeed.
- Listing and paging rules aren't defined in views. Listing, article and author markup each have their own view.

**Article**
- In order: main image as a banner, link back to the listing, title, date, tags, summary, main content, author section.
- The banner sits directly under the site header at full content width, matching the home banner. It may crop at the sides on narrow screens but keeps its subject visible. Its alt text is the media alt text, or the title if empty.
- Title, date, summary, main image and main content are required to publish. Editors set the date, and it doesn't change on edit or republish. The summary is plain text.
- 0-3 tags, shown as text labels and not links. No tags means no tag area.
- Main content uses the product block types and formatting rules, with at least one block. Saved headings are normalised (ADR-0025).

**Author**
- One author. Name, photo and bio are edited once, on an "About the author" tab on the listing page, and show at the foot of every article above the site footer, under the heading "About the author". There's no author page, and no other page shows the section.
- Name and photo are optional. An empty bio hides the section. The bio is plain text with line breaks kept.

**Seed**
- First boot imports, idempotently: the listing, author content and 10 published articles.
- Articles are humorous and coffee themed, in one author's voice, with distinct past dates. People and organisations named are fictional.
- Each has a summary, a distinct main image and a rich-text block of at least 150 words. At least 3 also have a feature list and a quote. At least one has 1 tag and at least one has 3.
- Main images are wide landscape banners. The author photo shows a gender-neutral coffee nerd, and the seeded author content doesn't assign a gender. All are managed SVG media with their own alt text, in a blog media folder.

**Layout**
- No horizontal scrolling at 390x844 or 1280x720. Pagination links are at least 44px tall on narrow screens.

## `layout-variants`: themes

ADR-0007, ADR-0031, ADR-0032, ADR-0033. The name dates from when these were page layouts.

- `demo` keeps the current light look. `theme/dark` and `theme/high-contrast` render the same catalogue, cart and checkout with a visibly different theme. Both are created from `demo` during the talk. Finished versions stay on `theme/dark-complete` and `theme/high-contrast-complete`, which meet every rule here and share a `main` commit with `demo`.
- A theme's diff touches only CSS files under `src/uShopDemo.Web/wwwroot/css/`. Views, scripts, seeded media, content and application code don't differ.
- A saved theme edit shows on refresh, with no restart or rebuild, and other instances don't change.
- Each theme supports the full flow: browse a category, view a product, add to cart, check out, confirmation. Each order exists only in that instance's database.
- At 1280x720, body text is at least 18px equivalent and nothing overflows horizontally.
- Dark theme text has a contrast ratio of at least 4.5:1 against its background.

High contrast, for low-vision users:
- All text is at least 7:1 against its background. Borders, form controls and focus outlines are at least 3:1.
- Links are underlined, no text sits on a gradient, and status and error messages don't rely on colour alone.
- Content and controls stay reachable and usable at 200% zoom from 1280x720.
- This covers the theme only and isn't a claim of WCAG conformance. Text baked into seeded images is out of scope.

## `demo-guide`

- Docs list prerequisites and a pre-talk checklist, so no image download or package restore is needed on stage.
- Docs describe the manual flow: from `demo`, create two worktrees, start them and the main checkout, open three sites, tear down.
- `docs/demo.md` has a paste-ready prompt per theme, with contrast-checked CSS values and the constraints (edit `site.css` only; no builds, commands or commits), and a one-command fallback to the finished theme.
- `demo` has a short `CLAUDE.md`, kept on `main` as `docs/demo-claude.md`, with the repo and theme rules and no project process. `docs/demo.md` lists the steps to cut and check `demo`.
- Docs are terse. Each fits on roughly one screen, except `docs/demo.md` and the reference pages (`docs/requirements.md`, `docs/implementation-notes.md`).
