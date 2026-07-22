# ADR-004: Transactional Outbox for Reliable Event Publishing

## Status

Accepted

## Date

2025-01-15

## Context

When a CFDI is generated, the system must:

1. Persist aggregate state changes to the database.
2. Publish domain events to the message broker.

If these two operations are not atomic, we risk:

- **Lost events**: Database commits but event publish fails → downstream systems never learn about the CFDI.
- **Ghost events**: Event publishes but database transaction rolls back → downstream systems act on non-existent data.

We need at-least-once delivery semantics without requiring distributed transactions (2PC).

## Decision

Implement the **Transactional Outbox** pattern directly in `McpCfdiDbContext.SaveChangesAsync`:

1. Before saving, collect all `DomainEvent` instances from tracked `IAggregateRoot` entities.
2. Serialize each event to an `OutboxMessage` row (Id, EventType, Payload as JSON, OccurredAt, ProcessedAt).
3. Save both aggregate changes and outbox messages in the **same database transaction**.
4. A background worker (or MassTransit outbox relay) reads unprocessed `OutboxMessage` rows and publishes them to the message broker, marking them as processed.

The `OutboxMessage` entity:

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
```

## Consequences

### Positive

- Guarantees at-least-once delivery: if the transaction commits, the event is guaranteed to eventually reach the broker.
- No distributed transactions — works with any relational database.
- Events are durably stored even if the message broker is down.
- Domain events are cleared from aggregates only after successful save.

### Negative

- At-least-once means consumers must be idempotent (they may receive the same event more than once).
- Additional database writes per transaction (one row per domain event).
- Requires a background process to poll/relay outbox messages.

### Mitigations

- Consumers use idempotency keys (event Id) to deduplicate.
- Outbox relay runs on a short polling interval or uses database notifications for near-real-time delivery.
