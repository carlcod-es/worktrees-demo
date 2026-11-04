# layout-variants Specification

## Purpose

Defines the three pre-built layout branches used for the manual (non-AI) demo and the rules that keep them comparable.

ADRs: ADR-0007, ADR-0020 (`docs/adrs/`).

## Requirements

### Requirement: Three distinct layout branches
The repository SHALL contain branches `layout/minimal`, `layout/catalog` and `layout/showcase`, each rendering the same catalogue, cart and checkout with a visibly different layout.

#### Scenario: Visual comparison
- **WHEN** all three branches run side by side
- **THEN** their home, category and product pages are distinguishable at a glance

### Requirement: Presentation-only diffs
The diff between any layout branch and `main` SHALL touch only views (including cart, checkout and confirmation views), CSS and static assets. Seeded media files under `wwwroot/media/` are content, not static assets, and SHALL NOT differ from `main`.

#### Scenario: Diff check
- **WHEN** `git diff --name-only main...layout/<name>` is run
- **THEN** every path is under the views or static asset directories, and none is under `wwwroot/media/`

### Requirement: Full flow works in every layout
Each layout SHALL support the complete flow: browse category, view product, add to cart, checkout, confirmation.

#### Scenario: Order in each layout
- **WHEN** an order is placed in each of the three running layouts
- **THEN** each shows a confirmation and each order exists only in that worktree's database

### Requirement: Responsive and legible
Each layout SHALL render usably at laptop and projector resolutions with text readable from a distance.

#### Scenario: Projector
- **WHEN** a page is viewed at 1280x720
- **THEN** body text is at least 18px equivalent and nothing overflows horizontally
