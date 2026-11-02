## Purpose

Independently hosted HTTP API that owns cart and order data for the demo shop, accepting writes asynchronously through a queue and serving reads from its own tables in the instance's database.

ADRs: ADR-0010, ADR-0011, ADR-0012, ADR-0014, ADR-0016 (`docs/adrs/`).

## ADDED Requirements

### Requirement: Independent service
The cart API SHALL run as a separate process from the Umbraco site, on its own HTTPS endpoint. It SHALL store its data in `ushop`-prefixed tables in the instance's single database, shared with Umbraco, with its own migrations history table. It SHALL NOT read or write Umbraco's tables, and the site SHALL NOT read or write the API's tables.

#### Scenario: Site stopped
- **WHEN** the web resource is stopped and the API keeps running
- **THEN** cart reads and writes against the API still succeed

#### Scenario: Separate tables in one database
- **WHEN** an instance is running
- **THEN** the instance has one database, cart and order tables in it are `ushop`-prefixed and created by the API's migrations, and the site has no code that accesses them

### Requirement: Cart identified by caller
Carts SHALL be addressed by a caller-supplied GUID in the URL. For a request with a valid session, a cart that has never been written SHALL read as empty, not as an error.

#### Scenario: Unknown cart
- **WHEN** a client with a valid session reads a cart id that has no stored data
- **THEN** the API returns an empty cart with status 200

### Requirement: Browser session
The API SHALL issue each browser a session: a random identifier of at least 128 bits, signed so the API can detect tampering, delivered in a cookie that is HttpOnly, Secure, SameSite=Strict, host-only and path `/`. The cookie name SHALL include the API's port so instances on the same host don't overwrite each other's cookie. A session endpoint SHALL return the existing session when the cookie is valid and issue a new one when it is missing, invalid or expired. Sessions SHALL be valid for 30 days from their last renewal and SHALL survive an API restart. Cart, operation and order endpoints SHALL return 401 without a valid session. Session identifiers SHALL NOT appear in response bodies or logs.

#### Scenario: First visit
- **WHEN** a browser without a session cookie calls the session endpoint
- **THEN** the response sets a new HttpOnly session cookie

#### Scenario: No session
- **WHEN** a client calls a cart endpoint without a session cookie
- **THEN** the API returns 401 and queues nothing

#### Scenario: Tampered cookie
- **WHEN** a client sends a session cookie whose value has been altered
- **THEN** cart endpoints return 401 and the session endpoint issues a new session

#### Scenario: API restart
- **WHEN** the API restarts with its database kept
- **THEN** existing session cookies remain valid and their carts readable

#### Scenario: Two instances on one machine
- **WHEN** one browser uses the shops of two worktrees
- **THEN** each instance sets and reads its own session cookie and neither invalidates the other

### Requirement: Cart ownership
A cart SHALL belong to the session of its first accepted write. Reading a cart owned by another session SHALL return an empty cart, identical to reading an unknown cart id. Writing to a cart owned by another session SHALL return 404 and queue nothing. Orders and operations SHALL be readable only by the session that created them; for any other session they SHALL return 404, identical to an unknown id. If two sessions race to create the same cart id, exactly one SHALL own it and the other's operation SHALL fail.

#### Scenario: Cart id leaked
- **WHEN** a second browser session uses another session's cart id
- **THEN** reading returns an empty cart, adding returns 404, and the original cart is unchanged

#### Scenario: Order number leaked
- **WHEN** a second browser session requests another session's order number
- **THEN** the API returns 404 and no customer details are disclosed

#### Scenario: Operation id leaked
- **WHEN** a second browser session requests another session's operation id
- **THEN** the API returns 404

#### Scenario: Race on a new cart id
- **WHEN** two sessions add to the same unused cart id at the same time
- **THEN** one session owns the cart and the other session's operation reports failed

### Requirement: Asynchronous writes
Adding a line, changing a line's quantity, removing a line and checking out SHALL be validated synchronously, then queued, and answered with `202 Accepted`, an operation id and a `Location` header for the operation status. The database SHALL be changed only by background processing of the queue. Writes for the same cart SHALL be applied in the order they were accepted.

#### Scenario: Add accepted
- **WHEN** a client posts a valid line to a cart
- **THEN** the response is 202 with an operation id, before the line is stored

#### Scenario: Ordered processing
- **WHEN** a client adds a product and immediately sets its quantity to 5
- **THEN** after both operations complete, the cart holds that product with quantity 5

#### Scenario: Invalid request
- **WHEN** a write has a quantity below 1 (add) or below 0 (update), above 99, or a missing product key, name or SKU, or a negative price
- **THEN** the API returns 400 with validation errors and queues nothing

### Requirement: Cart line semantics
A cart SHALL hold at most one line per product key. Adding an existing product SHALL increase its quantity, capped at 99. Setting quantity to 0 SHALL remove the line. Name, SKU and unit price SHALL be stored as supplied when the line is first added and not changed by later adds.

#### Scenario: Add twice
- **WHEN** a client adds 2 of a product and then 3 more
- **THEN** the cart has one line for that product with quantity 5

#### Scenario: Set to zero
- **WHEN** a client sets a line's quantity to 0
- **THEN** the line is removed

### Requirement: Caller-supplied product data
The API SHALL accept product name, SKU and unit price from the caller without verifying them against the catalogue. This is a deliberate demo simplification and SHALL be stated in the API's code and the README.

#### Scenario: Price from caller
- **WHEN** a client adds a line with unit price 1.00
- **THEN** the stored line has unit price 1.00

### Requirement: Operation status
The API SHALL expose the status of each accepted operation as `queued`, `completed` or `failed` (with a reason). Status SHALL be available for at least 10 minutes after acceptance while the API process is running.

#### Scenario: Poll to completion
- **WHEN** a client polls an accepted operation
- **THEN** it reports `queued` until processed and then `completed`

#### Scenario: Unknown operation
- **WHEN** a client requests an operation id the API did not issue
- **THEN** the API returns 404

### Requirement: Cart read
Reading a cart SHALL return its open lines (product key, name, SKU, unit price, quantity, line total), the item count and the grand total, reflecting all operations completed so far.

#### Scenario: Read after add completes
- **WHEN** an add operation for a cart has completed
- **THEN** reading the cart includes the line and the updated totals

### Requirement: Asynchronous checkout
Checkout SHALL require name, email (valid format), address and a non-empty card number, and SHALL NOT store or process the card number. On acceptance it SHALL return 202 with an order number. Processing SHALL convert the open cart into an order with the customer details, order time and total, after which the cart id reads as empty. If the cart is empty when processed, the order SHALL be rejected with a reason.

#### Scenario: Missing fields
- **WHEN** checkout is posted without an email
- **THEN** the API returns 400 with validation errors and no order number

#### Scenario: Place order
- **WHEN** checkout is accepted for a cart with lines
- **THEN** the response contains an order number, and once processed the order is stored and the cart reads as empty

#### Scenario: Empty cart
- **WHEN** checkout is processed for a cart with no lines
- **THEN** the order reads as `rejected` with a reason

### Requirement: Order status
Reading an order by order number SHALL return `pending` while its checkout is queued, `confirmed` with customer name, email, address, order time, lines and total once stored, or `rejected` with a reason. An order number the API never issued SHALL return 404.

#### Scenario: Poll order
- **WHEN** a client polls an order number right after checkout
- **THEN** it reads `pending`, then `confirmed` with the order details

### Requirement: Schema managed by migrations
Cart and order tables SHALL be created and evolved only by migrations applied automatically when the API starts, before it accepts requests.

#### Scenario: Fresh database
- **WHEN** the API starts against a new database
- **THEN** the latest migration is applied before the first request is served

### Requirement: Browser access restricted to the site
The API SHALL allow cross-origin browser requests, including credentials, only from the site's configured origin. Write requests with a body SHALL require `Content-Type: application/json` and SHALL be rejected with 415 otherwise, so that every cross-origin write needs a CORS preflight.

#### Scenario: Site origin
- **WHEN** the site's page calls the API from the browser
- **THEN** the request succeeds

#### Scenario: Other origin
- **WHEN** a page on any other origin calls the API from the browser
- **THEN** the browser blocks the response

#### Scenario: Form post from another site
- **WHEN** a cross-site HTML form posts to a cart write endpoint
- **THEN** the API rejects it (no session cookie is sent and the content type is not JSON) and nothing is queued
