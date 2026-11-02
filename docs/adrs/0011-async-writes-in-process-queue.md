# ADR-0011: Asynchronous writes through an in-process queue

- Status: Accepted
- Date: 2026-11-02 (recorded retrospectively 2026-11-02)
- Source: `openspec/changes/add-cart-api/design.md`

## Context

Cart and checkout writes must be accepted without waiting on a database write and applied per cart in acceptance order.

## Decision

Write endpoints validate synchronously, enqueue a command on a singleton bounded `Channel<CartCommand>` (capacity 1000, `FullMode = Wait`, single reader) and return `202 Accepted` with an operation id and `Location: /operations/{id}`. One `BackgroundService` processes commands in acceptance order, each in its own DI scope and `DbContext`. `OperationTracker` (singleton over `IMemoryCache`, 10-minute expiry) holds operation status and maps order number to operation. Checkout generates its order number (`US-yyMMdd-NNNNN`) at acceptance; a unique filtered index catches collisions.

## Alternatives

- Per-cart partitioned channels: more throughput, unnecessary for a demo.
- RabbitMQ or Azure Service Bus: rejected by the Product Owner; extra container per worktree.

## Consequences

- One reader gives per-cart ordering without locks; it is also the throughput ceiling (partitioning is the upgrade path).
- Queued commands, operation status and pending-order status are lost when the API stops; documented as a demo limitation.
- Reads can lag accepted writes; the client waits for the operation to complete before re-reading.
