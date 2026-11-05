# Follow-ups

Non-blocking items logged during reviews, grouped by change. Plan an item as a task before fixing it. Headings name the change that logged the item.

## `add-cart-api`

- Remove the `System.Security.Cryptography.Pkcs` and `.Xml` version pins once upstream packages are patched.
- Tests:
  - `Origin: null` → 403;
  - null check on the Scalar redirect `Location`;
  - duplicate wait helper in `SessionTests`;
  - duplicate older 401 test;
  - `CorsTests` disposes a client the factory owns;
  - no test that server and client money formatting match.
- `CheckoutTests` checks that the card number isn't stored in `Cart` string properties. Nothing checks logs or other tables.

## `add-shop-media`

- Gallery thumbnails load the full-size SVG (`Views/product.cshtml`); fine for small SVGs, revisit if raster images arrive.
- The current-thumbnail rule in `wwwroot/css/site.css` hard-codes `4px` in both the border and the `calc()`; changing one makes the thumbnail jump. Use a custom property.
- Product artwork bakes the product name and SKU into each SVG, so renaming a product leaves stale image text.
- `gallery.js` finds the main image with `gallery.querySelector`, which can match a nested gallery's main image; thumbnails already guard against nesting. Filter by `closest('[data-gallery]') === gallery`.
- Deleting a seeded media item (and emptying the recycle bin) or replacing its file in the backoffice deletes the tracked file under `wwwroot/media/ushop/`. Restore with `git checkout -- src/uShopDemo.Web/wwwroot/media/ushop`.

## `add-product-blocks`

- The rich-text block row label uses `stripHtml`, which joins paragraphs without a space ("ratios.USB-C"). Umbraco Flavored Markdown has no filter that keeps paragraph breaks.
- No automated tests cover `Shop/Content/HeadingNormaliser.cs` (h1 to h2, h4-h6 to h3, attributes and closing-tag whitespace, object and string values, malformed JSON, no change returns null). Needs a web test project, which is a shared `*.csproj` and needs explicit assignment.
- ADR-0025 says the normaliser runs during the uSync first-boot import. That's unverified, because no seeded markup has h1 or h4-h6.

## `add-mobile-nav`

- On mobile, the menu shows expanded (links stacked as 44px rows) until `nav.js` runs, then collapses. A small inline script in `<head>` that sets `data-nav-ready` early would remove the shift.
- `site.css` uses `max-width: 48rem` for the product and checkout single-column rule, but `width < 48rem` for the nav. At exactly 768px the nav is in desktop mode while those pages are single-column. Switch the older rule to `(width < 48rem)`.

## `add-blog`

- No automated tests cover `Shop/Blog/BlogListing.cs` (`Clamp` edge cases, date order, the title tie-break). Seed dates are distinct, so the tie-break has only been checked by reading and by hand. Needs the same web test project as `HeadingNormaliser`.

## `ushop-demo`

- `site.css` has rules for `.form-errors` and `.input-validation-error`, and no view or script uses either class. The high-contrast prompt in `docs/demo.md` and `theme/high-contrast-complete` also name `.input-validation-error`, so remove it from all three together.
- When the OpenSpec skills and tools are deleted from `main` (Product Owner, 2026-11-05), rewrite `CLAUDE.md` on `main` in the same change, because nothing on `main` will refer to them any more. Not before: `main` keeps the current `CLAUDE.md` until then. It's also the time to drop `src/uShopDemo.Web/Migrations/**` from the ownership table, as that path doesn't exist.
- The `.gitattributes` CRLF rule for `appsettings-schema.Umbraco.Cms.json` is tested on macOS only. Build in a fresh worktree on Windows and Linux and check `git status` stays clean.
- Telemetry (`ushop-demo` task 2.3, ADR-0034):
  - The site's OTLP sub-logger has no `MinimumLevel`, so it stays at Information if Umbraco's level is lowered. Fix: `.MinimumLevel.Verbose()` in `Extensions.cs`.
  - `AddServiceDefaults` also adds `Logging.AddOpenTelemetry` to the site, unused because Serilog replaces the logger factory. If that ever changes, site logs would appear twice.
  - `CartWorker` SQL spans have no recorded parent and are dropped, so an add-to-cart trace ends at the 202. An `ActivitySource` span around command handling would keep them; that also puts the checkout write's query text in traces.
  - `/umbraco/management` isn't excluded from traces, so an open backoffice adds noise.
  - API tests would export if the shell has `OTEL_EXPORTER_OTLP_ENDPOINT` set; `UseSetting` in `ApiFactory` closes that. No test asserts the "No endpoint" scenario, and `AddSerilogOtlpSink` has no coverage.
  - The API has no log config, so EF command and `/health` request logs reach Structured Logs at Information. The site sets the EF category to Warning.
  - The exporter-log filter is on the OTLP sub-logger only, so `System.Net.Http.HttpClient.Otlp*` lines still reach the site's console and file logs. An `Override` in `appsettings.json` would replace it.
  - `aspire otel` printed `[]` for a launcher-started run; the dashboard's `/api/telemetry` endpoints returned data.
- Kestrel logs `Unexpected exception in TimingPipeFlusher.FlushAsync` (`Win32Exception (14): Bad address`) on some HTTPS responses on macOS. Umbraco logs show it from 2026-11-01, before telemetry. Not investigated.
