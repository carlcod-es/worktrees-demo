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
Docs SHALL describe the manual demo: from `demo`, create two worktrees, start them and the main checkout, open all three sites, tear down.

#### Scenario: Follow the steps
- **WHEN** the steps are followed on a prepared machine
- **THEN** three isolated sites are running with distinct URLs

### Requirement: Agent flow
`docs/demo.md` SHALL provide one ready-to-paste prompt per theme, each with contrast-checked CSS values and the constraints (edit `site.css` only; no builds, commands or commits), and a one-command fallback to the finished theme.

#### Scenario: Agent run
- **WHEN** each prompt is given to a Claude session in its own worktree on `demo`
- **THEN** each theme shows on refresh in its own running instance, without touching the other worktree's files

#### Scenario: Agent stalls
- **WHEN** the fallback command is run in a worktree
- **THEN** the finished theme shows on refresh, without a restart

### Requirement: Stage branch rules
The `demo` branch SHALL have a short `CLAUDE.md`, kept on `main` as `docs/demo-claude.md`, describing the repo and the theme rules, with no project process (ADR-0033). `docs/demo.md` SHALL list the steps to cut and check `demo` after the final slice.

#### Scenario: Cut demo
- **WHEN** the steps are followed
- **THEN** `demo` differs from `main` only by the removed `openspec/` and `.claude/`, the replaced `CLAUDE.md` and the moved `docs/demo-claude.md`

### Requirement: Concise
Docs SHALL be terse and written for humans and AI readers, without filler.

#### Scenario: Review
- **WHEN** the docs are read
- **THEN** each page fits on roughly one screen, except `docs/demo.md`, which holds all the stage notes and prompts, and the reference pages `docs/requirements.md` and `docs/implementation-notes.md`
