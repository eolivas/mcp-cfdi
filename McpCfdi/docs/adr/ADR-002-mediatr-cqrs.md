# ADR-002: MediatR for CQRS

## Status

Accepted

## Date

2025-01-15

## Context

The CFDI generation flow involves orchestrating multiple steps: domain model construction, total calculations, XML serialization, cadena original generation, and digital signing. We need a way to:

- Separate command handling from API/transport concerns.
- Apply cross-cutting behaviours (logging, validation) uniformly without polluting handlers.
- Keep handlers focused on a single responsibility.

## Decision

Use **MediatR 12.4.1** as an in-process mediator implementing the CQRS pattern:

- **Commands** (e.g., `GenerarCfdiCommand`) represent write intentions and return results.
- **Queries** represent read intentions (to be added as the project evolves).
- **Pipeline Behaviours** provide cross-cutting concerns that execute before/after every handler.

Registered pipeline behaviours (in order):

1. `LoggingBehaviour<TRequest, TResponse>` — logs request name before and after handling.
2. `ValidationBehaviour<TRequest, TResponse>` — runs all registered FluentValidation validators; throws `ValidationException` on failure.

MediatR services are auto-registered from the Application assembly.

## Consequences

### Positive

- Handlers are single-responsibility: `GenerarCfdiCommandHandler` orchestrates CFDI generation without worrying about logging or validation.
- Adding new cross-cutting concerns (e.g., caching, metrics, authorization) requires only a new `IPipelineBehavior<,>` implementation.
- Clear separation between "what to do" (command) and "how to do it" (handler).

### Negative

- Adds a layer of indirection — you can't navigate directly from the API to business logic without understanding the mediator pattern.
- Pipeline behaviours run for all requests; fine-grained control requires conditional logic inside the behaviour.

### Mitigations

- Consistent naming convention: `XxxCommand` → `XxxCommandHandler` → `XxxCommandValidator`.
- Pipeline behaviours are registered in explicit order in `Program.cs`.
