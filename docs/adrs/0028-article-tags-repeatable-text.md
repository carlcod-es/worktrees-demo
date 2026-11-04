# ADR-0028: Article tags as a repeatable text list with a limit of 3

- Status: Accepted
- Date: 2026-11-04
- Source: `openspec/changes/add-blog/design.md`; Product Owner decision (tags are display-only)

## Context

Articles need at most 3 tags, shown as labels. The Product Owner decided there is no filtering or tag page. Umbraco's `Umbraco.Tags` editor has no maximum setting.

## Decision

- `blogArticle.tags` uses a new `Article tags` data type: `Umbraco.MultipleTextstring`, min 0, max 3. The editor enforces the limit with no application code, as `Feature items` does (ADR-0023).
- Views read the value as `IEnumerable<string>` and render plain text labels.

## Alternatives

- `Umbraco.Tags` plus a publishing handler that rejects a fourth tag: gives autocomplete, a shared tag list and tag queries, but needs application code for a feature the blog doesn't use. Worth it if filtering arrives.
- Checkbox list of fixed tags: consistent spelling, but editors can't add a tag without a data type change.

## Consequences

- No autocomplete, so editors can create near-duplicates such as "Moka pot" and "moka pot". Display-only tags make this cosmetic.
- Filtering by tag later would need a new ADR, probably moving to `Umbraco.Tags`. Stored values would need converting; the ephemeral database (ADR-0005) means only seed files change.
