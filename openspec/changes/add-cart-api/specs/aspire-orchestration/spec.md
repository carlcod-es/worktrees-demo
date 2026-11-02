ADRs: ADR-0003, ADR-0010 (`docs/adrs/`).

## MODIFIED Requirements

### Requirement: AppHost defines the full stack
The AppHost SHALL define a SQL Server container with two databases (Umbraco's and the cart API's), the Umbraco website and the cart API. The website SHALL wait for and reference Umbraco's database and SHALL start only after the cart API reports healthy; the cart API SHALL wait for and reference its own database and SHALL expose a health check that passes only when its database is reachable and migrated. The website SHALL receive the cart API's public URL and the cart API SHALL receive the website's origin. No resource outside the AppHost definition SHALL be required to run the stack.

#### Scenario: Cold start
- **WHEN** the AppHost is started in a clean checkout with Docker running
- **THEN** SQL Server starts, both databases are created, and the site and cart API start and become reachable without manual configuration

#### Scenario: Site waits for the API
- **WHEN** the AppHost starts
- **THEN** the website process is not started until the cart API's health check passes

#### Scenario: Site knows the API
- **WHEN** a site page is opened in the browser
- **THEN** the page calls the cart API instance belonging to the same AppHost

### Requirement: Per-instance port isolation
The AppHost dashboard, OTLP endpoint, resource service endpoint, website endpoint, cart API endpoint and SQL endpoint SHALL NOT be fixed to values shared across worktrees. Ports for the dashboard, OTLP, resource service, website and cart API SHALL be overridable by configuration; the SQL port SHALL be dynamically assigned.

#### Scenario: Concurrent AppHosts
- **WHEN** three AppHosts are started concurrently with distinct port configuration
- **THEN** all three start successfully, each dashboard lists only its own resources, and each site uses its own cart API
