# ADR-0034: Service defaults project and `Serilog.Sinks.OpenTelemetry`

- Status: Accepted
- Date: 2026-11-05
- Source: Product Owner decision during `ushop-demo` apply

## Context

The Aspire dashboard shows no logs, traces or metrics for the site or the cart API. Aspire gives each project the OTLP endpoint, but neither configures OpenTelemetry. Umbraco logs through Serilog, so a Microsoft.Extensions.Logging provider alone doesn't carry the site's logs. The `Observability-is-a-Superpower` repo solves this with a service defaults project and a custom sink that forwards Serilog events to the OpenTelemetry logger provider.

## Decision

- Add `src/uShopDemo.ServiceDefaults`, referenced by `uShopDemo.Web` and `uShopDemo.Api`, with `AddServiceDefaults` configuring logs, traces and metrics.
- Instrumentation: ASP.NET Core, HTTP client, runtime, SQL client. Health-check and static-file requests are not traced. SQL spans without a recorded parent are dropped, because Umbraco's background jobs would bury page traces.
- Export over OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, so tests and runs outside Aspire send nothing.
- Site logs go through `Serilog.Sinks.OpenTelemetry`, registered as an `ILogEventSink` that Umbraco's Serilog pipeline picks up. API logs use the OpenTelemetry logging provider, because the API doesn't use Serilog.
- OpenTelemetry packages stay on the version `Aspire.Microsoft.EntityFrameworkCore.SqlServer` already brings in (1.17.0).
- Left out: Azure Monitor, PII redaction, custom meters, service discovery, resilience handlers. Nothing here needs them.

## Alternatives

- Custom forwarding sink, as in `Observability-is-a-Superpower`: the Architect's recommendation, because it's known to work with Umbraco. The Product Owner chose the package: no custom sink code to own.
- Traces and metrics only: leaves the logs tab empty for the site.
- No telemetry: the stage flow doesn't use these tabs, but the dashboard looks broken if opened.

## Consequences

- Each instance's dashboard shows only its own telemetry, which suits the isolation story.
- Seven new direct package references, and one more project to build in every worktree.
- Two log paths: Serilog sink for the site, logging provider for the API.
- Theme branches are unaffected, because they differ only in `site.css`.
