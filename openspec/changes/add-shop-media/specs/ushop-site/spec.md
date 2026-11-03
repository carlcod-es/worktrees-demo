ADRs: ADR-0020, ADR-0021 (`docs/adrs/`).

## ADDED Requirements

### Requirement: Managed SVG media
Product images, category images and the site logo SHALL be media items managed in the backoffice, stored as SVG. Each image SHALL carry its own alt text, editable in the backoffice. Editors SHALL be able to upload an SVG file as a new media item. On first boot against an empty database, every seeded media item, with its file and alt text, SHALL be created without manual steps; the import SHALL be idempotent.

#### Scenario: Seeded media
- **WHEN** the site starts against an empty database
- **THEN** the backoffice media library contains every product, category and logo image, each with a non-empty alt text, and each image file loads

#### Scenario: SVG upload
- **WHEN** an editor uploads an SVG file in the backoffice media section
- **THEN** a media item is created and its file is served by the site

#### Scenario: Category images
- **WHEN** a visitor opens the home page
- **THEN** each category is shown with its managed image

### Requirement: Product gallery
Each seeded product SHALL have 3 or 4 images; the count MAY differ between products. Each image SHALL show a distinct view of the product. The product page SHALL show one main image and a thumbnail for every image, in the editor-defined order, with the first image as the initial main image. Choosing a thumbnail by pointer or keyboard SHALL make that image the main image without reloading the page and SHALL indicate the current thumbnail to sighted and assistive-technology users. The main image SHALL use the chosen image's alt text. Without script, the first image SHALL be the main image and each thumbnail SHALL link to its full image. Editors SHALL be able to add, remove and reorder a product's images in the backoffice.

#### Scenario: Swap by pointer
- **WHEN** a visitor clicks the third thumbnail on a product page
- **THEN** the main image becomes the third image with its alt text, the third thumbnail is marked current, and the page does not reload

#### Scenario: Swap by keyboard
- **WHEN** a visitor moves focus to a thumbnail with the keyboard and activates it
- **THEN** the main image changes to that image and focus stays on the thumbnail

#### Scenario: Without script
- **WHEN** a product page is loaded with script disabled
- **THEN** the first image is shown as the main image and following any thumbnail opens that image

#### Scenario: Varying count
- **WHEN** products with 3 and with 4 images are opened
- **THEN** each page shows exactly its own number of thumbnails without empty slots

#### Scenario: Editor reorders
- **WHEN** an editor moves a product's second image to first and publishes
- **THEN** the product page and the product's card show that image first

### Requirement: Site logo
The shop root SHALL have a logo image, seeded as an SVG wordmark. Every Umbraco-rendered page header SHALL show the logo as a link to the home page with the site title as its alt text. When no logo is set, the header SHALL show the site title as text, linked to the home page. Static error pages SHALL show a static copy of the same logo artwork, linked to the home page.

#### Scenario: Logo shown
- **WHEN** a visitor opens any shop page
- **THEN** the header shows the logo, its alt text is the site title, and activating it opens the home page

#### Scenario: No logo set
- **WHEN** an editor removes the logo from the shop root and publishes
- **THEN** the header shows the site title as a text link to the home page

#### Scenario: Static error page
- **WHEN** a request returns the static 500 or 4xx page
- **THEN** the page shows the logo linked to the home page

## MODIFIED Requirements

### Requirement: Seeded catalogue
On first boot against an empty database, the site SHALL import the shop content model and catalogue from uSync files (ADR-0006, ADR-0020): at least 4 categories and at least 12 products, each with name, SKU, price, summary, description and 3-4 images, and each category with an image. The import SHALL be idempotent.

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** the home page links to every category and every product has a working detail page

#### Scenario: Second start on same database
- **WHEN** the site restarts against an already seeded database
- **THEN** no duplicate content or media is created

### Requirement: Category listing
Each category page SHALL list only the products in that category, with name, price, the product's first image and a link to the product.

#### Scenario: Browse a category
- **WHEN** a visitor opens a category
- **THEN** only that category's products are shown

#### Scenario: Card image
- **WHEN** a visitor opens a category or the home page
- **THEN** each product card shows that product's first gallery image

### Requirement: Product detail
Each product page SHALL show name, SKU, price, description, the product gallery and an add-to-cart control with quantity. Adding to cart SHALL call the cart API from the browser without reloading the page, show that the add is in progress, and update the header cart count once the add completes.

#### Scenario: Add to cart
- **WHEN** a visitor adds 2 of a product
- **THEN** the page does not reload, and once the add completes the cart contains that product with quantity 2 and the header cart count reflects it

#### Scenario: API unavailable
- **WHEN** a visitor adds to cart while the cart API is unreachable
- **THEN** the page shows an error message and the cart count is unchanged

### Requirement: Friendly error pages
The site SHALL answer errors with friendly pages in every environment, including Development, and SHALL NOT show exception details in the browser. Unknown URLs SHALL return 404 with an Umbraco-managed not-found page rendered through the shared layout. Unhandled exceptions SHALL return 500 with a static error page. Other 4xx responses SHALL keep their status code and show a static error page. Adding an unknown or tampered product to the cart SHALL return 400 and change nothing. Static error pages are exempt from the branch badge and show the static logo copy (requirement "Site logo"). Backoffice paths (`/umbraco`) SHALL keep Umbraco's own error handling.

#### Scenario: Unknown URL
- **WHEN** a visitor opens a URL that matches no content
- **THEN** the response is 404 and the page shows the site navigation and branch badge

#### Scenario: Server error
- **WHEN** a page request fails with an unhandled exception (for example the database is unavailable)
- **THEN** the response is 500 and the page is the static error page with no stack trace

#### Scenario: Tampered add-to-cart
- **WHEN** an add-to-cart post carries a product key that is not a published product
- **THEN** the response is 400 with the static error page and the cart is unchanged

#### Scenario: Backoffice unaffected
- **WHEN** the backoffice is used after these pages are configured
- **THEN** login and editing work and backoffice API errors are returned by Umbraco unchanged

### Requirement: Presentation confined to views and static assets
The shop's appearance SHALL be defined only by Razor views and static CSS/assets. Markup rendered from cart API data SHALL come from templates defined in the views, so layouts can change it. Content model, seeded media files, the cart and gallery client scripts, cart API, migrations and application code SHALL NOT change between layouts.

#### Scenario: Layout swap
- **WHEN** only views and CSS differ between two checkouts
- **THEN** both render the same catalogue, images, gallery behaviour and cart behaviour with different layouts
