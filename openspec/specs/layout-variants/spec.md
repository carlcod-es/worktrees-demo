# layout-variants Specification

## Purpose

Defines the three themes shown on stage: the current look on the `demo` branch, plus two theme branches made live in parallel worktrees, and the rules that keep them quick to make and comparable.

ADRs: ADR-0007, ADR-0020, ADR-0031, ADR-0032, ADR-0033 (`docs/adrs/`).

## Requirements

### Requirement: Three distinct themes
The `demo` branch SHALL keep the current light look. Branches `theme/dark` and `theme/high-contrast` SHALL each render the same catalogue, cart and checkout with a visibly different theme. Both are created from `demo` during the talk and are not kept in the repository beforehand. Finished versions SHALL be kept on `theme/dark-complete` and `theme/high-contrast-complete`, which meet every requirement here and share a `main` commit with `demo`.

#### Scenario: Visual comparison
- **WHEN** `demo` and both theme branches run side by side
- **THEN** their home, category and product pages are distinguishable at a glance

### Requirement: CSS-only diffs
The diff between either theme branch and `demo` SHALL touch only CSS files. Views, scripts, seeded media under `wwwroot/media/`, content and application code SHALL NOT differ.

#### Scenario: Diff check
- **WHEN** `git diff --name-only demo...theme/<name>` is run
- **THEN** every path is a CSS file under `src/uShopDemo.Web/wwwroot/css/`

### Requirement: Live edit without restart
A saved theme edit SHALL show in a running instance on browser refresh, without restarting or rebuilding that instance.

#### Scenario: Refresh on stage
- **WHEN** a theme's CSS is saved in a worktree whose instance is running
- **THEN** the next page load shows the new theme and the other instances are unchanged

### Requirement: Full flow works in every theme
Each theme SHALL support the complete flow: browse category, view product, add to cart, checkout, confirmation.

#### Scenario: Order in each theme
- **WHEN** an order is placed in each of the three running instances
- **THEN** each shows a confirmation and each order exists only in that instance's database

### Requirement: Responsive and legible
Each theme SHALL render usably at laptop and projector resolutions with text readable from a distance. Text in the dark theme SHALL have a contrast ratio of at least 4.5:1 against its background.

#### Scenario: Projector
- **WHEN** a page is viewed at 1280x720
- **THEN** body text is at least 18px equivalent and nothing overflows horizontally

### Requirement: High contrast for low vision
The `theme/high-contrast` theme SHALL give all text a contrast ratio of at least 7:1 against its background, and borders, form controls and focus outlines at least 3:1. Links SHALL be underlined, text SHALL NOT sit on a gradient, and status and error messages SHALL NOT rely on colour alone. This requirement covers the theme only: it is not a claim that the site conforms to WCAG. Text baked into seeded images, including the logo, is out of scope, because seeded media can't differ.

#### Scenario: Contrast check
- **WHEN** the theme's colour pairs are measured
- **THEN** text pairs are at least 7:1 and border, control and focus pairs are at least 3:1

#### Scenario: Zoom
- **WHEN** a page is viewed at 200% browser zoom from 1280x720
- **THEN** all content and controls remain reachable and usable

#### Scenario: Links without colour
- **WHEN** a page with links in body text is viewed
- **THEN** those links are underlined
