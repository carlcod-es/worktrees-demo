## Purpose

Defines the Umbraco demo shop that is the subject of the talk: automatically installed and populated, with a working cart and dummy checkout, and presentation isolated to files that vary per layout.

ADRs: ADR-0002, ADR-0006, ADR-0007, ADR-0008, ADR-0009 (`docs/adrs/`).

## ADDED Requirements

### Requirement: Unattended install
The site SHALL install Umbraco against the Aspire-provided database on first boot with a known demo backoffice login, without any installer UI.

#### Scenario: First boot
- **WHEN** the site starts against an empty database
- **THEN** Umbraco is installed and the backoffice accepts the documented demo credentials

### Requirement: Seeded catalogue
On startup against a database lacking shop content, the site SHALL create the shop content model, at least 4 categories and at least 12 products, each with name, SKU, price, summary, description and image. Seeding SHALL be idempotent.

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
Each product page SHALL show name, SKU, price, description, image and an add-to-cart control with quantity.

#### Scenario: Add to cart
- **WHEN** a visitor adds 2 of a product
- **THEN** the cart contains that product with quantity 2 and the header cart count reflects it

### Requirement: Persistent cart
The cart SHALL be identified by a cookie and stored in the site database. Visitors SHALL be able to add, change quantity, remove lines and see line totals and a grand total. The unit price SHALL be captured when the line is added.

#### Scenario: Cart survives app restart
- **WHEN** a visitor adds items and the web resource is restarted (database kept)
- **THEN** the same browser sees the same cart

#### Scenario: Remove line
- **WHEN** a visitor removes a line
- **THEN** the line disappears and the total updates

#### Scenario: Empty cart
- **WHEN** the cart has no lines
- **THEN** the cart page says it is empty and checkout is unavailable

### Requirement: Dummy checkout
Checkout SHALL collect name, email, address and a fake card number, perform no payment, store the details on the cart, mark it as an order with a generated order number, clear the visitor's active cart and show a confirmation page with the order number and total.

#### Scenario: Place order
- **WHEN** a visitor submits checkout with a non-empty cart and required fields filled
- **THEN** a confirmation page shows an order number and the order is stored in the database

#### Scenario: Missing fields
- **WHEN** required fields are empty
- **THEN** the form is shown again with validation messages and no order is created

### Requirement: Schema managed by migrations
Shop tables SHALL be created and evolved only by EF Core migrations, applied automatically on startup before the site serves requests.

#### Scenario: Fresh database
- **WHEN** the site starts against a new database
- **THEN** all shop tables exist with the latest migration applied

### Requirement: Clearly a demo
Every page SHALL display a visible notice that this is a demo shop and no real orders or payments are processed.

#### Scenario: Any page
- **WHEN** any shop page is viewed
- **THEN** the demo notice is visible

### Requirement: Presentation confined to views and static assets
The shop's appearance SHALL be defined only by Razor views and static CSS/assets. Seeding, content model, cart logic, migrations and application code SHALL NOT change between layouts.

#### Scenario: Layout swap
- **WHEN** only views and CSS differ between two checkouts
- **THEN** both render the same catalogue and cart behaviour with different layouts

### Requirement: Layout identification
Every page SHALL display the name of the current git branch in a small badge so parallel instances are distinguishable on screen.

#### Scenario: Three tabs open
- **WHEN** three layout instances are open side by side
- **THEN** each shows its own branch name
