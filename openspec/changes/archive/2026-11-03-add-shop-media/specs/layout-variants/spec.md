ADRs: ADR-0020 (`docs/adrs/`).

## MODIFIED Requirements

### Requirement: Presentation-only diffs
The diff between any layout branch and `main` SHALL touch only views (including cart, checkout and confirmation views), CSS and static assets. Seeded media files under `wwwroot/media/` are content, not static assets, and SHALL NOT differ from `main`.

#### Scenario: Diff check
- **WHEN** `git diff --name-only main...layout/<name>` is run
- **THEN** every path is under the views or static asset directories, and none is under `wwwroot/media/`
