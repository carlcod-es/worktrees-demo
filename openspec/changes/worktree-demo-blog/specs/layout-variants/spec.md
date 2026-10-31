## Purpose

Defines the three pre-built layout branches used for the manual (non-AI) demo and the rules that keep them comparable.

## ADDED Requirements

### Requirement: Three distinct layout branches
The repository SHALL contain branches `layout/minimal`, `layout/magazine` and `layout/sidebar`, each rendering the same content with a visibly different layout.

#### Scenario: Visual comparison
- **WHEN** all three branches run side by side
- **THEN** their home pages are distinguishable at a glance

### Requirement: Presentation-only diffs
The diff between any layout branch and `main` SHALL touch only views, CSS and static assets.

#### Scenario: Diff check
- **WHEN** `git diff --name-only main...layout/<name>` is run
- **THEN** every path is under the views or static asset directories

### Requirement: Responsive and legible
Each layout SHALL render usably at laptop and projector resolutions with text readable from a distance.

#### Scenario: Projector
- **WHEN** a page is viewed at 1280x720
- **THEN** body text is at least 18px equivalent and nothing overflows horizontally
