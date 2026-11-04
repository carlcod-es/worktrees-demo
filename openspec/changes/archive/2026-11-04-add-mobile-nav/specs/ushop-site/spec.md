## ADDED Requirements

### Requirement: Mobile navigation
Below 768px viewport width, every Umbraco-rendered page header SHALL hide the category links behind a button labelled "Menu", and SHALL keep the logo and cart link visible. Activating the button by pointer or keyboard SHALL show or hide the category links without reloading the page, and the button SHALL expose its expanded or collapsed state to assistive technology. Pressing Escape while the menu is open SHALL close it and move focus to the button. The current category SHALL be indicated in the open menu as it is inline. At 768px and wider, the category links SHALL show inline and the button SHALL NOT be shown. Without script, the category links SHALL be shown and the button SHALL NOT be shown.

#### Scenario: Collapsed on a phone
- **WHEN** a visitor opens any shop page at 390x844
- **THEN** the header shows the logo, a "Menu" button and the cart link, the category links are hidden, and nothing overflows horizontally

#### Scenario: Open and close
- **WHEN** a visitor at 390x844 activates "Menu" by tap or by Enter or Space
- **THEN** the category links show, the button reports expanded, and activating it again hides them and the button reports collapsed

#### Scenario: Escape closes
- **WHEN** the menu is open and the visitor presses Escape
- **THEN** the menu closes and focus is on the "Menu" button

#### Scenario: Wide screen
- **WHEN** a visitor opens any shop page at 1280x720
- **THEN** the category links show inline and there is no "Menu" button

#### Scenario: No script
- **WHEN** a visitor opens any shop page at 390x844 with script disabled
- **THEN** the category links show and there is no "Menu" button

## MODIFIED Requirements

### Requirement: Presentation confined to views and static assets
The shop's appearance SHALL be defined only by Razor views and static CSS/assets. Markup rendered from cart API data SHALL come from templates defined in the views, so layouts can change it. Content model, seeded media files, the cart, gallery and navigation client scripts, cart API, migrations and application code SHALL NOT change between layouts.

#### Scenario: Layout swap
- **WHEN** only views and CSS differ between two checkouts
- **THEN** both render the same catalogue, images, gallery behaviour, navigation behaviour and cart behaviour with different layouts
