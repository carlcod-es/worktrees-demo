## ADDED Requirements

### Requirement: Price display
All prices and totals SHALL display in pounds sterling in English format (`£1,234.56`): product cards, product and category pages, cart lines and totals, checkout and order confirmation. Server-rendered and script-rendered amounts SHALL format identically. Stored prices SHALL NOT change; only the currency shown changes. The site language SHALL remain `en-US`.

#### Scenario: Server and client agree
- **WHEN** a product priced 1234.5 is shown on its product page and then added to the cart
- **THEN** both the product page and the cart line show `£1,234.50`

## MODIFIED Requirements

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

### Requirement: Presentation confined to views and static assets
The shop's appearance SHALL be defined only by Razor views and static CSS/assets. Markup rendered from cart API data SHALL come from templates defined in the views, so layouts can change it. Content model, the cart client script, cart API, migrations and application code SHALL NOT change between layouts.

#### Scenario: Layout swap
- **WHEN** only views and CSS differ between two checkouts
- **THEN** both render the same catalogue and cart behaviour with different layouts

## REMOVED Requirements

### Requirement: Schema managed by migrations
**Reason**: The site no longer has shop tables; cart and order data moved to the cart API's database.
**Migration**: See `cart-api` requirement "Schema managed by migrations".
