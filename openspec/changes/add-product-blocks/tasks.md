## 1. Content model and seeding (worker-backend; explicit assignment of `src/uShopDemo.Web/uSync/**`)

Contract for section 2: element type and property aliases in `design.md` "Content model contract".

- [x] 1.1 Spike. Add data types `Product rich text` (ADR-0024), `Feature items`, `Category intro` and `Product description blocks`. Add element types `richTextBlock`, `featureListBlock` and `quoteBlock` in an `Element types` folder, with labels and icons from `design.md`. Move `product.description` to the Block List and rename it "Description". Convert Brewing Scale only, with one block of each type. Verify:
  - a fresh `node scripts/dev.mjs` import reports no failed items;
  - after a uSync export of Brewing Scale from the dashboard, the `description` value is equivalent to the hand-authored one (keys and values match; ignore order and noise);
  - in the backoffice, each row shows icon, type and snippet;
  - pasting HTML containing a table, an image, coloured text, an h1 and an h4 into a rich-text block keeps none of them after save;
  - saving a quote with no text, or a feature list with no items, is rejected with a message;
  - the 161-character intro check is moved to task 1.2.

  Revert any export-only noise before committing. Report each check's result; escalate if paste or label checks fail (`design.md` "Risks").
- [x] 1.2 Convert the other 11 product descriptions to one rich-text block each, keeping the same HTML. Add a feature list and a quote to Ethiopia Yirgacheffe and Hand Grinder. Move `category.intro` to `Category intro`: mandatory, with mandatory message "Enter an intro of up to 160 characters." Depends on 1.1. Verify:
  - `grep -L "Umbraco.BlockList\|contentData" uSync/v18/Content/<products>` finds no product without block content;
  - a fresh run imports with no failed items, and a restart against the same database creates no duplicates;
  - saving a category with an empty intro, or a 161-character intro, is rejected with a message (if `maxChars` doesn't block the save, apply the regex fallback from `design.md`).
- [x] 1.3 Editor guidance: set the `<Description>` of all 7 page types, all 16 page-type properties and every element type and its properties to the text in `design.md` "Editor guidance text". Check each line against the views and report any wording change. Depends on 1.2 (same files). Verify: `grep -rn "<Description></Description>\|<Description><!\[CDATA\[\]\]></Description>" src/uShopDemo.Web/uSync/v18/ContentTypes` finds nothing, and a fresh import reports no failed items.

- [x] 1.4 Heading normaliser (worker-backend; own paths `src/uShopDemo.Web/Shop/**`), per `design.md` "Heading normaliser" and ADR-0025. Depends on 1.1. Verify:
  - `dotnet build uShopDemo.slnx` succeeds;
  - on a fresh run, saving Brewing Scale via the Management API with a rich-text block containing `<h1>A</h1><h4 class="x">B</h4><h2>C</h2>` stores `<h2>A</h2><h3 class="x">B</h3><h2>C</h2>` (read back via the API);
  - feature list and quote values are unchanged;
  - non-product content saves are unaffected.

- [x] 1.5 (Product Owner request after `add-product-blocks` task 4.1) Give the other 9 seeded products a feature list block and a quote block after their rich-text block, matching the showcase products. Copy is fixed in the Architect's brief. Stoneware Mug's feature list has no heading.

## 2. Block views and styles (worker-frontend)

Depends on 1.1 (aliases and seeded Brewing Scale). Can run in parallel with 1.2 and 1.3 (disjoint files).

- [x] 2.1 Add `Views/Partials/blocklist/default.cshtml` and `Views/Partials/blocklist/Components/{richTextBlock,featureListBlock,quoteBlock}.cshtml` with the markup contract in `design.md` "Rendering". In `product.cshtml`, replace the `Html.Raw` description with the Block List render, wrapped in `<div class="description">` only when it has items. Verify:
  - `dotnet build uShopDemo.slnx` succeeds;
  - `grep -n "Html.Raw" src/uShopDemo.Web/Views/product.cshtml` finds nothing;
  - on a fresh run, `curl -sk <site>/<brewing-scale-url>` contains one each of `block-richtext`, `block-features` and `block-quote`, in editor order;
  - a product whose description is emptied in the backoffice and republished renders no `class="description"`.
- [x] 2.2 CSS in `wwwroot/css/site.css` for `.description` headings, lists and links, `.block-features`, and `.block-quote` (with `white-space: pre-line` on the quote text). Text at least 18px. Verify: `dotnet build uShopDemo.slnx` succeeds, and the three seeded showcase product pages show no horizontal overflow at 1280x720 or 390x844.

## 3. Docs (Architect; explicit paths)

Depends on 2.1.

- [x] 3.1 Write `docs/content-blocks.md`, one screen covering:
  - the block types and their editor rules;
  - the markup contract;
  - "layouts override component views, not `default.cshtml`";
  - how to add a block type on `main`.

  In `README.md`, add one line in the content section pointing to it, and cite ADR-0023, ADR-0024 and ADR-0025 in the `ushop-site` spec's ADR list at archive. Verify: both files read on one screen, and every path they name exists.

## 4. Verification (Product Owner, Firefox)

- [x] 4.1 (Product Owner: passed, "good enough"; the Save-path and label risks were accepted without a separate check) On a fresh `node scripts/dev.mjs`:
  - the three showcase products show rich text, a feature list and a quote, and the other 9 products show their original text;
  - in the backoffice, add, reorder and remove blocks on a product, publish, and see the product page follow;
  - block rows show a snippet: a feature list with a heading shows the heading; clear the heading of one feature list (no need to publish) and its row shows the items instead. If not, report it; the Architect switches the label to `Feature list: {umbValue: heading} {umbValue: items}`;
  - every field shows a description;
  - a 161-character intro and an empty intro are both rejected;
  - an intro of `Fresh <b>beans</b>` shows literally on the category page and the home page.
