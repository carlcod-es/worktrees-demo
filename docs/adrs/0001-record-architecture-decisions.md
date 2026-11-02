# ADR-0001: Record architecture decisions as ADRs

- Status: Accepted
- Date: 2026-11-02

## Context

Technology decisions were made in OpenSpec `design.md` files during propose, mixed with implementation detail. There was no single record of what was decided, why, and what replaced what.

## Decision

Record each architecture decision as an ADR in `docs/adrs/NNNN-title.md` with sections Status, Context, Decision, Alternatives, Consequences. Specs and designs reference the ADRs that govern them. ADRs are immutable: a changed decision gets a new ADR that supersedes the old one, and the old one's status changes to "Superseded by ADR-NNNN".

ADR-0002 to ADR-0014 were written retrospectively on 2026-11-02 from `openspec/changes/ushop-demo/design.md` and `openspec/changes/add-cart-api/design.md`. Their dates are the date of the source design. Their alternatives are only those recorded in that design.

## Alternatives

- Decisions only in `design.md`: they are scattered across changes and archived along with them.

## Consequences

- Propose writes ADRs alongside `design.md`; `design.md` keeps the implementation detail.
- Superseded decisions stay readable with their original rationale.
