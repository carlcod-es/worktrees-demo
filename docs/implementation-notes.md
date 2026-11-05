# Implementation notes

Detail and findings that the code doesn't explain. Decisions are in `docs/adrs/`, and requirements are in `docs/requirements.md`.

## Umbraco and uSync findings

- Views are compiled at build. Umbraco 18 has no runtime Razor compiler without `Umbraco.Cms.DevelopmentMode.Backoffice`, so view edits need a launcher restart.
- Template files are named by alias, `Views/<alias>.cshtml`, in lower camel case. Umbraco rewrites them, adding a BOM, when it creates templates.
- uSync won't create a template whose `.cshtml` is missing: it logs `Failed to create template` and writes no stub. Add a placeholder view before importing.
- Razor generics need `@(...)` wrapping.
- `ModelsMode` is `Nothing`, so views read properties by alias with `Value<T>()`.
- Mandatory, minimum and length rules apply on publish, not on save, as tested through the Management API.
- Tabs on a page type need `Type` `Tab`, not `Group`, or the backoffice also shows a "Generic" tab.
- Heading levels can't be limited in the editor, so h1 and h4-h6 survive a paste and `HeadingNormaliser` fixes them on save. Pasting into the rich-text editor drops tables, images, colour, underline and strike.
- Hand-authored uSync: prove one item on a fresh boot before writing the rest. If a file won't import, upload that item in the backoffice and use its export as the template.
  - Media items use fixed keys, and `umbracoBytes` must match the file size.
  - Content refers to media by key, in the MediaPicker3 value format.
  - Block List values use the `layout` / `contentData` / `settingsData` / `expose` shape, as in `uSync/v18/Content/brewing-scale.config`.
- `uSync/v18` is authoritative for seeded content and editor descriptions.

## Errors

`Program.cs` handles errors on site paths only, skipping `/umbraco`. `UseExceptionHandler` and `UseStatusCodePages` send the static file directly and keep the status. Path re-execution isn't used, because the static file middleware would reset the status to 200.

## Instances and ports

- Port block: SHA-1 of the real, lower-cased worktree path gives a slot from 0 to 99, and the block starts at `15000 + slot * 10`. Offsets: site 0, dashboard 1, OTLP 2, resource service 3, API 4. There's no state file.
- Aspire randomises container name suffixes and SQL host ports. Resources are prefixed `ushop-`, so the container is `ushop-sql-<suffix>`. The database stays `umbracoDbDSN`, because that's the connection string name Umbraco reads.
- `web.WaitFor(api)` with `WithHttpHealthCheck` means the API creates its tables before Umbraco's unattended install runs on the same database.
- If the API stops later, the site keeps running and the browser shows "Could not reach the cart service".
- Background shells ignore SIGINT. Stop a backgrounded AppHost with SIGTERM to its own PID, which also removes its SQL container.

## Cart API

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/session` | | 204, sets or renews the cookie |
| GET | `/carts/{cartId}` | | 200 cart |
| POST | `/carts/{cartId}/lines` | `productKey, name, sku, unitPrice, quantity` | 202 `{ operationId }` |
| PUT | `/carts/{cartId}/lines/{productKey}` | `quantity` (0 removes) | 202 `{ operationId }` |
| DELETE | `/carts/{cartId}/lines/{productKey}` | | 202 `{ operationId }` |
| POST | `/carts/{cartId}/checkout` | `name, email, address, cardNumber` | 202 `{ operationId, orderNumber }` |
| GET | `/operations/{operationId}` | | 200 `{ status, error? }` or 404 |
| GET | `/orders/{orderNumber}` | | 200 `{ status, ... }` or 404 |
| GET | `/health` | | 200 when the database is reachable |

- **Flow:** writes (add, update, remove) are validated, queued in-process and answered 202 with an operation id. The page polls the operation, then re-reads the cart. Checkout returns an order number at once; the order reads `pending`, then `confirmed` or `rejected`, and the confirmation page polls it.
- **Line keys:** lines are keyed by product key, because the browser knows it before the add is processed.
- **Sessions:** a signed HttpOnly cookie `__Host-ushop-session-<apiPort>`, lasting 30 days. Stateless, with no table and no write on issue. Middleware resolves the session before body binding, so a missing session is reported before any 415 or 400. Write endpoints carry "any content type" metadata so routing doesn't return its own 415 first. Keys are in `ushopDataProtectionKeys`, one set per instance.
- **Cookies on `localhost`:** browsers don't separate cookies by port, so the port is in the name. Site and API ports count as same-site, so SameSite=Strict cookies are still sent on the page's `fetch` calls.
- **Ownership:** the worker re-checks each command's session. Inserting a cart that another session just created hits the primary key and fails the operation, which settles the race. Rejecting a foreign write with 404 reveals that the id is in use, which is acceptable for random GUIDs.
- **Queue:** one bounded channel (capacity 1000, waits when full) with a single reader, so commands apply in acceptance order without locks. Each runs in a fresh DI scope. A failed command marks its operation failed, and the worker continues.
- **Status:** `OperationTracker` is in memory, with a 10-minute expiry, and also maps order numbers to operations. So `GET /orders/{n}` checks the stored order, then the tracked checkout, then returns 404.
- **Order numbers:** `US-yyMMdd-NNNNN`, generated at acceptance so the 202 can return one.
- **CSRF:** there are no antiforgery tokens. Protection comes from SameSite=Strict, the JSON-only rule (which forces a preflight), the CORS origin check, no writes over GET, and the origin check on `POST /session`, which also allows the API's own origin for Scalar.
- **Tests:** `WebApplicationFactory` with SQLite in-memory, a named shared-cache database per factory held open by a keep-alive connection. Each `DbContext` opens its own connection, because `SqliteConnection` isn't thread-safe alongside the worker. SQLite uses `EnsureCreatedAsync()`, because the migrations are SQL Server-specific.

## Client scripts

Plain ES modules with no framework or build step, loaded from `_Layout` with `asp-append-version`. Views own the markup, through data-attribute hooks: `docs/cart-hooks.md`, `docs/gallery-hooks.md`, `docs/nav-hooks.md`.

- **`cart.js` session and id:** calls `POST /session` once per page load, before other calls, with `credentials: 'include'`. The cart id is in `localStorage["ushop.cartId"]`, which is per origin, so instances don't share carts. A 404 on a write means the cart was reset, and it gets a new id.
- **`cart.js` writes:** each one polls its operation until it completes, up to 15 seconds, then re-reads the cart. Buttons get `aria-busy` while waiting.
- **Money:** `Intl.NumberFormat('en-GB', ...)` in the script and `Money.Format` with an explicit `en-GB` culture on the server, because the site language stays `en-US`.
- **`nav.js`:** doesn't read the viewport; CSS decides what shows. So resizing can't leave the menu stuck closed. Escape is handled on `document`, because a tap can leave focus on the body in Firefox and Safari on macOS. The menu is a disclosure, not a modal, so there's no focus trap.

## Media and artwork

- Seeded files are under `wwwroot/media/ushop/`: `products/<sku-lower>-<n>.svg`, `categories/<alias>.svg`, `blog/` and `logo.svg`. `wwwroot/img/logo.svg` is a byte-identical copy for the static error pages.
- Artwork is flat, hand-authored SVG, with no script, event-handler attributes, external references or `<foreignObject>`.
- Product images are 800x600, using a palette per category. Each shows a different view: front, angle or side, detail, in use or with packaging.
- Blog banners are 1600x500, with the subject in the central 640x460. The author photo is 800x600.
- Alt text describes the image: `<product name>, <view>`.
- Views render media only through `<img>`, where SVG script doesn't run. Uploaded SVGs aren't sanitised, which is accepted for a localhost demo with one trusted admin. A file opened directly would run its script.

## Blog

- `BlogListing.ForPage` is the only source of which articles appear where. `Clamp` is pure, and zero items gives page 1 of 1.
- `publishDate` only sets order and display, so a future date doesn't hide an article. Canonical links and other SEO markup are out of scope.
- The author is Perry Kolator, a fictional coffee obsessive with a dry voice. Copy uses no gendered pronouns for Perry, and quotes are attributed to fictional people.
- The heading normaliser covers `product.description` and `blogArticle.mainContent`.

## Content blocks

- `Views/Partials/blocklist/default.cshtml` is a project-owned copy. Override only the component views, so new block types keep rendering.
- Feature list items are repeatable textstrings, which give per-item add, remove and reorder without an overlay per item.

## Themes

- A theme edits `wwwroot/css/site.css`: the `:root` colour variables, the hero gradient and the input colours, which are hard-coded. The branch badge stays as it is.
- `[hidden] { display: none !important; }` must stay, because `nav.js` and `cart.js` rely on it.
- Error pages load the same CSS, so themes apply to them too.
- A saved CSS edit is served with no restart, and the `asp-append-version` hash changes.
