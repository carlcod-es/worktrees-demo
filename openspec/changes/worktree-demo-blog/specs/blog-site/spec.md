## Purpose

Defines the Umbraco blog that is the subject of the demo: automatically installed, automatically populated, and with presentation isolated to files that vary per layout.

## ADDED Requirements

### Requirement: Unattended install
The site SHALL install Umbraco against the Aspire-provided database on first boot with a known demo backoffice login, without any installer UI.

#### Scenario: First boot
- **WHEN** the site starts against an empty database
- **THEN** Umbraco is installed and the backoffice accepts the documented demo credentials

### Requirement: Seeded blog content
On startup against a database lacking the blog content, the site SHALL create the blog content model and at least five sample posts with title, date, author, summary, body and image. Seeding SHALL be idempotent.

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** the home page lists the sample posts and each post has a working page

#### Scenario: Second start on same database
- **WHEN** the site restarts against an already seeded database
- **THEN** no duplicate content is created

### Requirement: Presentation confined to views and static assets
The blog's appearance SHALL be defined only by Razor views and static CSS/assets. Seeding, content model and application code SHALL NOT change between layouts.

#### Scenario: Layout swap
- **WHEN** only views and CSS differ between two checkouts
- **THEN** both render the same content with different layouts

### Requirement: Layout identification
Every page SHALL display the name of the current git branch (or "main") in a small badge so parallel instances are distinguishable on screen.

#### Scenario: Three tabs open
- **WHEN** three layout instances are open side by side
- **THEN** each shows its own branch name
