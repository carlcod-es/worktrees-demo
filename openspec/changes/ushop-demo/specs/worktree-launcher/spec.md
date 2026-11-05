## Purpose

Defines the Node scripts that make running and managing parallel worktrees one command each, on Windows, macOS and Linux.

ADRs: ADR-0004 (`docs/adrs/`).

## ADDED Requirements

### Requirement: Stable per-worktree ports
`scripts/dev.mjs` SHALL derive a port block from the worktree's path so the same worktree gets the same ports on every run, and SHALL probe for availability and shift to the next free block when any port is taken.

#### Scenario: Repeat run
- **WHEN** a worktree is started twice in succession with no conflicts
- **THEN** the website and dashboard URLs are identical both times

#### Scenario: Collision
- **WHEN** the derived port is already in use by another process
- **THEN** the launcher selects a different free block and prints the resulting URLs

### Requirement: Launcher output
The launcher SHALL print the website URL, dashboard URL, branch and worktree path before starting, and SHALL pass ports and branch name to the AppHost via configuration.

#### Scenario: Start
- **WHEN** `node scripts/dev.mjs` is run in a worktree
- **THEN** the URLs are printed and the AppHost runs in the foreground until interrupted

### Requirement: Worktree management
`scripts/worktrees.mjs` SHALL provide commands to create worktrees for the demo branches (creating missing branches from the branch checked out in the main worktree), list them with their URLs, and remove them.

#### Scenario: Create the theme worktrees
- **WHEN** the `up` command is run
- **THEN** a sibling directory exists for each of the two theme branches, checked out as worktrees, and new branches start from the branch checked out in the main worktree

#### Scenario: Cleanup
- **WHEN** the `down` command is run
- **THEN** the demo worktrees are removed and the branches are retained

### Requirement: Cross-platform
Scripts SHALL use only Node built-ins and path APIs, with no shell-specific syntax.

#### Scenario: Windows
- **WHEN** the scripts are run on Windows, macOS or Linux
- **THEN** they behave identically
