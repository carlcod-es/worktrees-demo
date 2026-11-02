## MODIFIED Requirements

### Requirement: Launcher output
The launcher SHALL print the website URL, cart API URL, dashboard URL, branch and worktree path before starting, and SHALL pass ports (including the cart API port) and branch name to the AppHost via configuration.

#### Scenario: Start
- **WHEN** `node scripts/dev.mjs` is run in a worktree
- **THEN** the site, cart API and dashboard URLs are printed and the AppHost runs in the foreground until interrupted
