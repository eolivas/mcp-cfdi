# ADR-003: MassTransit for Async Messaging

## Status

Accepted

## Date

2025-01-15

## Context

The system raises domain events (e.g., when a CFDI is generated). These events need to be published to downstream bounded contexts (Notifications, Identity) without coupling the core domain to specific transport mechanisms.

We need:

- A transport-agnostic abstraction for publishing events.
- Support for multiple transports (InMemory for development, RabbitMQ for production).
- Reliable delivery guarantees when paired with the transactional outbox.

## Decision

Use **MassTransit 8.2.5** as the messaging abstraction:

- `IPublishEndpoint` is used to publish domain events.
- `MassTransitEventPublisher` implements the application-layer `IApplicationEventPublisher` interface, publishing domain events through MassTransit's publish endpoint.
- Transport is configured in `Program.cs`:
  - **Development**: InMemory transport for fast local feedback.
  - **Production**: RabbitMQ (configurable via connection string).

## Consequences

### Positive

- Domain and Application layers are completely transport-agnostic — they only know about `IApplicationEventPublisher`.
- Switching from InMemory to RabbitMQ requires only a configuration change in the composition root.
- MassTransit provides consumer retry, error queues, and message serialization out of the box.
- Integration with the transactional outbox ensures events are not lost even if the message broker is temporarily unavailable.

### Negative

- MassTransit adds complexity compared to a simple `IEventBus` with manual RabbitMQ client.
- InMemory transport does not replicate all production behaviors (no network failures, no message persistence).

### Mitigations

- Integration tests can use the InMemory transport for speed.
- Contract tests validate message schemas between producer and consumer.
