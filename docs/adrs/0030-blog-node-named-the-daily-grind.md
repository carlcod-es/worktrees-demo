# ADR-0030: Blog node named "The Daily Grind", with URLs and nav label from the name

- Status: Accepted
- Date: 2026-11-04
- Source: Product Owner decision during `add-blog` task 2.3
- Supersedes: ADR-0027 (clause "Article URLs are `/blog/<article-name>/`" only), ADR-0029 (clause "Page URLs are `/blog/` and `/blog/?page=N`. Links to page 1 use `/blog/`." only)

## Context

ADR-0027, ADR-0029 and the `blog` spec assumed a node named `Blog`, with "The Daily Grind" only as its title. The seeded node was named "The Daily Grind", and the Product Owner chose to keep that name in the URL and the main navigation.

## Decision

- The `blog` node is named "The Daily Grind".
- Listing URLs are `/the-daily-grind/` and `/the-daily-grind/?page=N`. Links to page 1 omit the query string. Article URLs are `/the-daily-grind/<article-name>/`.
- The nav link prints the node's name, as category links do, so it reads "The Daily Grind".
- Views build links with `Url()` and don't hard-code the path.

## Alternatives

- Node named `Blog`, URL `/blog/` and nav label "Blog", with "The Daily Grind" as the page title only: more obvious to visitors. Rejected by the Product Owner.
- Node named "The Daily Grind" with a fixed "Blog" nav label: a label editors can't change, which doesn't match the page heading.

## Consequences

- Renaming the node in the backoffice changes the nav label and, unless a redirect or alias is set, the URLs.
- The label doesn't say "blog", so the name alone has to tell visitors what the link is.
