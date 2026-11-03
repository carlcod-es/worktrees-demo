# ushop-site Specification

## Purpose

Defines the Umbraco demo shop that is the subject of the talk: automatically installed and populated, with a working cart and dummy checkout, and presentation isolated to files that vary per layout.

ADRs: ADR-0002, ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0015 (`docs/adrs/`).

## Requirements

### Requirement: Unattended install
The site SHALL install Umbraco against the Aspire-provided database on first boot with a known demo backoffice login, without any installer UI.

#### Scenario: First boot
- **WHEN** the site starts against an empty database
- **THEN** Umbraco is installed and the backoffice accepts the documented demo credentials

### Requirement: Seeded catalogue
On first boot against an empty database, the site SHALL import the shop content model and catalogue from uSync files (ADR-0006): at least 4 categories and at least 12 products, each with name, SKU, price, summary, description and image. The import SHALL be idempotent.

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** the home page links to every category and every product has a working detail page

#### Scenario: Second start on same database
- **WHEN** the site restarts against an already seeded database
- **THEN** no duplicate content is created

### Requirement: Category listing
Each category page SHALL list only the products in that category, with name, price, image and a link to the product.

#### Scenario: Browse a category
- **WHEN** a visitor opens a category
- **THEN** only that category's products are shown

### Requirement: Product detail
Each product page SHALL show name, SKU, price, description, image and an add-to-cart control with quantity. Adding to cart SHALL call the cart API from the browser without reloading the page, show that the add is in progress, and update the header cart count once the add completes.

#### Scenario: Add to cart
- **WHEN** a visitor adds 2 of a product
- **THEN** the page does not reload, and once the add completes the cart contains that product with quantity 2 and the header cart count reflects it

#### Scenario: API unavailable
- **WHEN** a visitor adds to cart while the cart API is unreachable
- **THEN** the page shows an error message and the cart count is unchanged

### Requirement: Persistent cart
The cart SHALL be identified by an id stored in the visitor's browser and held by the cart API, and SHALL be reachable only with the browser session that created it. The page SHALL obtain a session from the cart API before its first cart request. Visitors SHALL be able to add, change quantity, remove lines and see line totals and a grand total, without page reloads. The unit price SHALL be captured when the line is added. The header cart count SHALL be loaded from the cart API on every page.

#### Scenario: Cart survives site restart
- **WHEN** a visitor adds items and the web resource is restarted (API and its database kept)
- **THEN** the same browser sees the same cart

#### Scenario: Cart id used in another browser
- **WHEN** another browser is given the same cart id
- **THEN** it sees an empty cart and cannot change the original

#### Scenario: Remove line
- **WHEN** a visitor removes a line
- **THEN** once the removal completes the line disappears and the total updates, without a page reload

#### Scenario: Empty cart
- **WHEN** the cart has no lines
- **THEN** the cart page says it is empty and checkout is unavailable

### Requirement: Dummy checkout
Checkout SHALL collect name, email, address and a fake card number and submit them to the cart API from the browser. No payment SHALL be performed. Validation errors returned by the API SHALL be shown next to the form without a reload. On acceptance the browser SHALL go to the confirmation page for the returned order number, which SHALL show the order as being processed until the API confirms it, then show the order number, lines and total, or the rejection reason.

#### Scenario: Place order
- **WHEN** a visitor submits checkout with a non-empty cart and required fields filled
- **THEN** the confirmation page shows the order as processing, then the order number and total once confirmed

#### Scenario: Missing fields
- **WHEN** required fields are empty
- **THEN** validation messages are shown on the checkout page and no order is created

### Requirement: Friendly error pages
The site SHALL answer errors with friendly pages in every environment, including Development, and SHALL NOT show exception details in the browser. Unknown URLs SHALL return 404 with an Umbraco-managed not-found page rendered through the shared layout. Unhandled exceptions SHALL return 500 with a static error page. Other 4xx responses SHALL keep their status code and show a static error page. Adding an unknown or tampered product to the cart SHALL return 400 and change nothing. Static error pages are exempt from the branch badge. Backoffice paths (`/umbraco`) SHALL keep Umbraco's own error handling.

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
The shop's appearance SHALL be defined only by Razor views and static CSS/assets. Markup rendered from cart API data SHALL come from templates defined in the views, so layouts can change it. Content model, the cart client script, cart API, migrations and application code SHALL NOT change between layouts.

#### Scenario: Layout swap
- **WHEN** only views and CSS differ between two checkouts
- **THEN** both render the same catalogue and cart behaviour with different layouts

### Requirement: Layout identification
Every page SHALL display the name of the current git branch in a small badge so parallel instances are distinguishable on screen.

#### Scenario: Three tabs open
- **WHEN** three layout instances are open side by side
- **THEN** each shows its own branch name

### Requirement: Price display
All prices and totals SHALL display in pounds sterling in English format (`£1,234.56`): product cards, product and category pages, cart lines and totals, checkout and order confirmation. Server-rendered and script-rendered amounts SHALL format identically. Stored prices SHALL NOT change; only the currency shown changes. The site language SHALL remain `en-US`.

#### Scenario: Server and client agree
- **WHEN** a product priced 1234.5 is shown on its product page and then added to the cart
- **THEN** both the product page and the cart line show `£1,234.50`
