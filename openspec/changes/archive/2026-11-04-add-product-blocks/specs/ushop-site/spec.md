## ADDED Requirements

### Requirement: Product description blocks
A product's description SHALL be composed by editors from blocks (Product Owner constraint: Umbraco Block List) instead of HTML typed into a text field. The block types SHALL be:

- **Rich text**: formatted text. Content is required.
- **Feature list**: an optional heading and one or more short text items.
- **Quote**: quote text (required) and an optional attribution.

Editors SHALL be able to add, edit, reorder, repeat and remove blocks, with no limit on count. The description SHALL be optional; when it has no blocks, the product page SHALL show no description area. The product page SHALL render the blocks in editor order. In the editor, each block SHALL show as a row with an icon, its block type and a snippet of its content, with no visual preview of the site styling. Each block type SHALL render separately in the views, so a layout can restyle one block type without changing the others (requirement "Presentation confined to views and static assets").

#### Scenario: Compose a description
- **WHEN** an editor adds a rich-text block, a feature list with 3 items and a quote to a product, then publishes
- **THEN** the product page shows the three blocks in that order

#### Scenario: Reorder blocks
- **WHEN** an editor moves the quote above the rich-text block and publishes
- **THEN** the product page shows the quote first

#### Scenario: Empty description
- **WHEN** an editor removes every block from a product and publishes
- **THEN** the product page shows no description area and the rest of the page is unchanged

#### Scenario: Required block content
- **WHEN** an editor tries to save a quote block with no quote text, or a feature list with no items
- **THEN** the block is rejected with a message saying what is missing

#### Scenario: Editor overview
- **WHEN** an editor opens a product with several blocks
- **THEN** each block shows as a row with its icon, type name and a snippet of its content

### Requirement: Rich-text formatting
The rich-text block SHALL allow only bold, italic, links, bulleted and numbered lists, and two heading levels below the page title. Tables, images, embedded media, colours and font choices SHALL NOT be available, and the product page SHALL NOT render them, including when pasted in.

#### Scenario: Allowed formatting
- **WHEN** an editor writes a heading, a bulleted list, a link and bold text in a rich-text block and publishes
- **THEN** the product page shows each with its formatting

#### Scenario: Pasted disallowed formatting
- **WHEN** an editor pastes content containing a table, an image and coloured text into a rich-text block and publishes
- **THEN** the product page shows no table, image or colour

### Requirement: Category intro
A category's intro SHALL be required plain text of at most 160 characters. It is shown on the category page and as the category teaser on the home page. Markup typed into the intro SHALL be shown as literal text and never rendered.

#### Scenario: Intro too long
- **WHEN** an editor enters an intro longer than 160 characters and saves
- **THEN** the save is rejected with a message giving the limit

#### Scenario: Intro missing
- **WHEN** an editor clears a category's intro and saves
- **THEN** the save is rejected with a message that the intro is required

#### Scenario: Markup in intro
- **WHEN** an editor saves the intro `Fresh <b>beans</b>` and publishes
- **THEN** the category page and home page show the text `Fresh <b>beans</b>` literally

### Requirement: Editor field guidance
Every page type, every property on a page type and every property on a block type SHALL have an editor description of one or two short sentences, saying where it is used on the site and what to enter (for example expected length, format or whether it is optional).

#### Scenario: Property guidance
- **WHEN** an editor opens any shop page or block in the backoffice
- **THEN** every property shows a non-empty description

#### Scenario: Page type guidance
- **WHEN** an editor creates a new page under the shop
- **THEN** each offered page type shows a non-empty description

## MODIFIED Requirements

### Requirement: Seeded catalogue
On first boot against an empty database, the site SHALL import the shop content model and catalogue from uSync files (ADR-0006, ADR-0020): at least 4 categories and at least 12 products, each with name, SKU, price, summary, description and 3-4 images, and each category with an image and an intro. Each seeded product description SHALL contain at least one rich-text block. At least 2 seeded products SHALL also contain a feature list block and a quote block. The import SHALL be idempotent.

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** the home page links to every category and every product has a working detail page

#### Scenario: Second start on same database
- **WHEN** the site restarts against an already seeded database
- **THEN** no duplicate content or media is created

#### Scenario: Every block type seeded
- **WHEN** the site starts with an empty database
- **THEN** at least 2 product pages each show a rich-text block, a feature list and a quote

### Requirement: Product detail
Each product page SHALL show name, SKU, price, the description blocks (requirement "Product description blocks"), the product gallery and an add-to-cart control with quantity. Adding to cart SHALL call the cart API from the browser without reloading the page, show that the add is in progress, and update the header cart count once the add completes.

#### Scenario: Add to cart
- **WHEN** a visitor adds 2 of a product
- **THEN** the page does not reload, and once the add completes the cart contains that product with quantity 2 and the header cart count reflects it

#### Scenario: API unavailable
- **WHEN** a visitor adds to cart while the cart API is unreachable
- **THEN** the page shows an error message and the cart count is unchanged
