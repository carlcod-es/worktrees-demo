# demo-guide Specification

## Purpose

Defines the documentation that lets the presenter, or an AI agent, run the demo reliably.

## Requirements

### Requirement: Prerequisites and setup
Docs SHALL list prerequisites and a pre-talk checklist including pulling the SQL Server image in advance.

#### Scenario: Offline-ish prep
- **WHEN** the checklist is followed
- **THEN** no image download or package restore is needed on stage

### Requirement: Manual flow
Docs SHALL describe the manual demo: create three worktrees, start each, open all three sites, tear down.

#### Scenario: Follow the steps
- **WHEN** the steps are followed on a prepared machine
- **THEN** three isolated sites are running with distinct URLs

### Requirement: Agent flow
Docs SHALL provide a ready-to-paste agent prompt that asks for three shop layouts in three parallel worktrees, including the constraints (presentation-only files, use the launcher, report URLs).

#### Scenario: Agent run
- **WHEN** the prompt is given to an agent that supports parallel worktrees
- **THEN** it produces three branches with three running instances without touching each other's files

### Requirement: Concise
Docs SHALL be terse and written for humans and AI readers, without filler.

#### Scenario: Review
- **WHEN** the docs are read
- **THEN** each page fits on roughly one screen
