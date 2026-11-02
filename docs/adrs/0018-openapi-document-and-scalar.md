# ADR-0018: OpenAPI document and Scalar UI for the cart API, Development only

- Status: Accepted
- Date: 2026-11-02
- Source: `openspec/changes/add-cart-api/design.md` (added by Product Owner decision)

## Context

The cart API (ADR-0010) has no machine-readable description and no way to browse or try its endpoints other than `curl`. Since .NET 9, ASP.NET Core generates OpenAPI documents itself (`Microsoft.AspNetCore.OpenApi`); the templates no longer include Swashbuckle. .NET 10 emits OpenAPI 3.1 by default. ASP.NET Core ships no UI.

## Decision

- `Microsoft.AspNetCore.OpenApi`: `AddOpenApi()`, `MapOpenApi()` serving `/openapi/v1.json`.
- `Scalar.AspNetCore`: `MapScalarApiReference()` serving `/scalar`, reading that document.
- Both mapped only when the environment is Development (the Aspire default for project resources). Neither requires a session.
- Scalar is served from the API origin, so the session cookie applies: calling `POST /session` in Scalar lets the other endpoints be tried there.

## Alternatives

- Swashbuckle (generator and UI): no longer in the templates; duplicates the built-in generator.
- Built-in generator plus Swashbuckle's SwaggerUI package: works; older UI, no advantage here.
- Built-in generator plus Redoc: read-only, no "try it".
- Document only, no UI: no third-party package, but nothing to browse.

## Consequences

- Two new package references in `src/uShopDemo.Api`, one third-party (Scalar).
- Endpoint metadata (names, summaries, produces/accepts) affects the document; the write endpoints' "any content type" accepts metadata (`add-cart-api` design.md, CORS and CSRF) must not hide the JSON request bodies.
- Not available outside Development; the demo never runs in another environment.
