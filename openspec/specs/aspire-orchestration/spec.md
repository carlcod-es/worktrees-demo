# aspire-orchestration Specification

## Purpose

Defines the Aspire AppHost topology and the isolation guarantees that let several instances of the same app run concurrently from different git worktrees.

ADRs: ADR-0003, ADR-0004, ADR-0005 (`docs/adrs/`).

## Requirements

### Requirement: AppHost defines the full stack
The AppHost SHALL define a SQL Server container with one database shared by the Umbraco website and the cart API, the website and the cart API. The website SHALL wait for and reference the database and SHALL start only after the cart API reports healthy; the cart API SHALL wait for and reference the same database and SHALL expose a health check that passes only when its database is reachable and migrated. The website SHALL receive the cart API's public URL and the cart API SHALL receive the website's origin. No resource outside the AppHost definition SHALL be required to run the stack.

#### Scenario: Cold start
- **WHEN** the AppHost is started in a clean checkout with Docker running
- **THEN** SQL Server starts, the database is created, and the site and cart API start and become reachable without manual configuration

#### Scenario: Site waits for the API
- **WHEN** the AppHost starts
- **THEN** the website process is not started until the cart API's health check passes

#### Scenario: Site knows the API
- **WHEN** a site page is opened in the browser
- **THEN** the page calls the cart API instance belonging to the same AppHost

### Requirement: Per-instance data isolation
Each AppHost instance SHALL use its own SQL Server container and database, with no persistent volume shared between instances or runs.

#### Scenario: Two worktrees at once
- **WHEN** AppHosts are running from two different worktrees
- **THEN** each has a distinct SQL Server container and content created in one is not visible in the other

#### Scenario: Restart is clean
- **WHEN** an AppHost is stopped and started again
- **THEN** the database is recreated empty and re-seeded

### Requirement: Per-instance port isolation
The AppHost dashboard, OTLP endpoint, resource service endpoint, website endpoint, cart API endpoint and SQL endpoint SHALL NOT be fixed to values shared across worktrees. Ports for the dashboard, OTLP, resource service, website and cart API SHALL be overridable by configuration; the SQL port SHALL be dynamically assigned.

#### Scenario: Concurrent AppHosts
- **WHEN** three AppHosts are started concurrently with distinct port configuration
- **THEN** all three start successfully, each dashboard lists only its own resources, and each site uses its own cart API

### Requirement: Frictionless local access
When started by the launcher, the dashboard, website and every Aspire endpoint SHALL use HTTPS on localhost with the trusted ASP.NET development certificate, in every worktree, and the dashboard SHALL open without a login token. No endpoint SHALL accept plain HTTP.

#### Scenario: Open dashboard
- **WHEN** the launcher starts an AppHost
- **THEN** its HTTPS dashboard URL opens directly without a login prompt or certificate warning

#### Scenario: Any worktree
- **WHEN** AppHosts run from several worktrees on different port blocks
- **THEN** each site and dashboard is served over HTTPS with the same trusted certificate

### Requirement: Telemetry in the dashboard
When started by an AppHost, the site and the cart API SHALL send logs, traces and metrics to that AppHost's dashboard (ADR-0034). With no OTLP endpoint configured, they SHALL export nothing.

#### Scenario: Open the dashboard
- **WHEN** pages are browsed and an item is added to the cart in a running instance
- **THEN** that instance's dashboard shows structured logs, traces and metrics for both `ushop-web` and `ushop-api`

#### Scenario: Other instances
- **WHEN** several instances run at once
- **THEN** each dashboard shows telemetry from its own instance only

#### Scenario: No endpoint
- **WHEN** the API tests run with no OTLP endpoint
- **THEN** nothing is exported and the tests pass
